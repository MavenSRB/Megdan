using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's two co-moves - This Way! (Blue II) and That Way! (Blue III) - which print
    /// one sentence between them and differ only in how far it reaches. Milestone 8.71e.
    ///
    /// <para><b>The sentence.</b> "A friendly hero in radius chooses a distance of 1 or 2 [1, 2 or 3];
    /// move both of you that number of spaces in the same direction of your choice." This Way! adds the
    /// printed reminder "( Both must be moved the full distance, or neither one moves )"; That Way!
    /// prints no reminder and has the same rule, because that is what the reminder is reminding of.</para>
    ///
    /// <para><b>Three questions in printed order, and two of them belong to different seats.</b> The
    /// friendly hero is chosen by Hanu (p.19's Target, so p.18's immunity applies, and p.17's
    /// "Friendly" keeps Hanu himself out of his own disk); the DISTANCE is chosen by that hero on their
    /// own seat, which is p.10's "You make all the choices in your card's text, unless the card text
    /// instructs the other hero to make a choice" and this text does; the DIRECTION is Hanu's, because
    /// the sentence says "of your choice". HA-Q9.</para>
    ///
    /// <para><b>The distances are a modal, not a list of numbers - HA-Q13e.</b> The plan for this block
    /// was a new capability offering the printed distances as Integer candidates to the named friend,
    /// with a distance no direction admits greyed. That last half cannot be built that way:
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.PendingEffectChoice"/> refuses an
    /// unavailable candidate that is not an option - "A capability's value candidates are already the
    /// set it derived, so one it offered is one it can act on" - and
    /// <see cref="EffectChoiceRequest"/> gives a handler no unavailable set to fill, so such a handler
    /// could only have dropped the impossible distance in silence. A printed "chooses a distance of 1
    /// or 2" IS a two-bullet modal whose chooser the text names, so it is composed as one:
    /// <see cref="EffectNodeKind.Choose"/> under <see cref="EffectChooserRole.BoundHero"/> - Whisper's
    /// "An enemy hero in range chooses one" and Cutter's X Marks the Spot, shipped since milestone
    /// 8.35z33 - with one <c>coupled-direct-move@1</c> per printed distance. Milestone 6.5ac then greys
    /// a distance whose every direction is blocked, milestone 6.5ae's stop closes a modal whose every
    /// distance is blocked (p.10's stop, asked of the player rather than taken for them), and every one
    /// of those readings is the engine's own rather than a second one written here.</para>
    ///
    /// <para><b>Both move the full distance or neither</b> is <c>coupled-direct-move@1</c>'s whole
    /// contract - Widget's Drag Off is the precedent - and binding <c>leastDistance</c> and
    /// <c>distance</c> to the SAME number is what makes one bullet one exact distance. The capability
    /// asks the actor for a destination among every (direction, distance) pair the board admits, so at
    /// equal bounds that offer is exactly the directions legal at the distance the friend named. p.11
    /// governs both units: no obstacle transit, no ending on an obstacle, and obstacles are not
    /// ignored, so <c>ignoresObstacles</c> is false on every bullet.</para>
    ///
    /// <para><b>The pair is read as two SPACES rather than two heroes</b>, which is what the coupled
    /// move takes: Hanu's own square through <c>actor-space@1</c>, and the friend's through the
    /// selection's <c>chosenHeroSpace</c> output. Both are read before the modal opens, so the
    /// feasibility walk that greys a bullet can see them - milestone 8.35l's carried outcomes - and the
    /// capability re-derives everything from the live board when the answer arrives.</para>
    ///
    /// <para><b>The friend selection declares no consumer.</b> Milestone 6.8l would narrow it by the
    /// node that consumes it, and there is no single such node here: the distances are separate
    /// bullets, and a bullet's feasibility is not the modal's. Narrowing by the 1-space bullet would be
    /// wrong rather than merely partial, because <c>CoupledMoveQuery.CanMove</c> checks TRANSIT at
    /// every step and an END only at the last, so a pair blocked at 1 in every direction can still be
    /// legal at 2. A friend for whom no distance works is therefore offered, and the modal that follows
    /// greys all of its bullets and closes on p.10's stop - which is the shard's own reading of what
    /// happens when every distance is infeasible.</para>
    /// </summary>
    public static partial class HanuEffectContent
    {
        /// <summary>The sequence root both faces walk.</summary>
        public const string CoMoveRootNode = "hanu-co-move-root";

        /// <summary>Where Hanu is standing - the first half of the coupled pair.</summary>
        public const string CoMoveActorSpaceNode = "hanu-co-move-actor-space";

        /// <summary>"A friendly hero in radius" - p.9's disk, his own side, never himself.</summary>
        public const string CoMoveFriendNode = "hanu-co-move-friend";

        /// <summary>"chooses a distance of ..." - the modal the friend answers on their own seat.</summary>
        public const string CoMoveDistanceNode = "hanu-co-move-distance";

        /// <summary>The 1-space bullet, which both faces print.</summary>
        public const string CoMoveOneSpaceNode = "hanu-co-move-one-space";

        /// <summary>The 2-space bullet, which both faces print.</summary>
        public const string CoMoveTwoSpacesNode = "hanu-co-move-two-spaces";

        /// <summary>The 3-space bullet, which That Way! alone prints.</summary>
        public const string CoMoveThreeSpacesNode = "hanu-co-move-three-spaces";

        /// <summary>Where the pair's first member - Hanu - ends up.</summary>
        public const string CoMoveActorAtOutput = "firstAt";

        /// <summary>Where the pair's second member - the friendly hero - ends up.</summary>
        public const string CoMoveFriendAtOutput = "secondAt";

        /// <summary>How far they both went, which is the number the friend named.</summary>
        public const string CoMoveDistanceOutput = "distance";

        // MERGE NOTE (8.71 wave): `ActorSpaceOutput` ("space") is declared once, publicly, in
        // HanuEffectContent.Greens.cs; this partial's private copy was dropped at the squash-merge.

        /// <summary>
        /// The printed distances, per face: This Way! prints 1 and 2, That Way! prints 1, 2 and 3.
        /// The bullet node each of them is drawn as is fixed here so the labels, the definitions and
        /// the fixtures all name the same node.
        /// </summary>
        private static readonly string[] DistanceNodes =
        {
            CoMoveOneSpaceNode, CoMoveTwoSpacesNode, CoMoveThreeSpacesNode
        };

        /// <summary>
        /// One of the two faces, as the one thing that differs between them: how many printed distances
        /// the friend may choose among. <paramref name="greatestDistance"/> is 2 for This Way! and 3 for
        /// That Way!, and the bullets are always 1 upward, because both faces print "a distance of 1"
        /// as their first option.
        /// </summary>
        public static EffectDefinition CoMove(
            CardDefinitionId cardId, string contentRevision, int greatestDistance)
        {
            var root = new EffectNodeId(CoMoveRootNode);
            var actorSpace = new EffectNodeId(CoMoveActorSpaceNode);
            var friend = new EffectNodeId(CoMoveFriendNode);
            var modal = new EffectNodeId(CoMoveDistanceNode);
            var bullets = Enumerable.Range(0, greatestDistance)
                .Select(index => new EffectNodeId(DistanceNodes[index]))
                .ToArray();

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { actorSpace, friend, modal }),

                // p.19's exchange of nothing: this reads where he stands and moves nobody, which is
                // why the walk below can see the pair before the modal opens.
                new EffectNodeDefinition(
                    actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(ActorSpaceOutput, EffectValueKind.Coordinate)
                    }),

                new EffectNodeDefinition(
                    friend, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        // The enclosing action is the Skill this text IS (p.12), and p.18 declares
                        // immunity per activity, so the offer asks immunity the question the action
                        // itself asks.
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),

                        // "A friendly hero" - p.17's own side.
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AlliedWithSource),

                        // Neither face prints "not adjacent to you", so nothing is taken out of the
                        // disk's innermost ring - the friend standing beside him is the commonest
                        // subject this card has.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),

                        // p.17: "A friendly unit means another unit on your team. It never includes
                        // you." His own side does NOT exclude him, so this parameter is what does -
                        // which is why `HanuCoMoveTests` proves it by flipping it rather than by
                        // observing that he happens to be absent.
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),

                        // The coupled move names two SPACES, so the selection publishes where its
                        // answer is standing as well as who they are.
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                new EffectNodeDefinition(
                    modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: bullets,
                    inputs: new[]
                    {
                        // "A friendly hero ... chooses": the modal says which node named its chooser,
                        // and the resolver sends the question to that hero's seat.
                        EffectInputBinding.FromNodeOutput(
                            EffectNodeDefinition.ChooserParameterName, friend,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    chooser: EffectChooserRole.BoundHero,
                    minimumSelections: 1,
                    maximumSelections: 1)
            };

            nodes.AddRange(bullets.Select((bullet, index) => Bullet(bullet, actorSpace, friend, index + 1)));
            return new EffectDefinition(CoMoveEffectId(cardId), contentRevision, root, nodes);
        }

        /// <summary>
        /// One printed distance: "move both of you that number of spaces in the same direction of your
        /// choice", at exactly <paramref name="distance"/>.
        ///
        /// <para>The two bounds are the same number, which is what makes this bullet that distance and
        /// nothing else - Widget's Drag Off binds 2 and 3 to the same node for a face whose sentence
        /// really does admit a range. The capability then offers the actor every destination the board
        /// admits, which at equal bounds is one square per legal direction, and moves both objects or
        /// neither.</para>
        /// </summary>
        private static EffectNodeDefinition Bullet(
            EffectNodeId id, EffectNodeId actorSpace, EffectNodeId friend, int distance) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticCoupledMoveCapabilities.MoveV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "first", actorSpace, ActorSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromNodeOutput(
                        "second", friend,
                        SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("leastDistance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),

                    // p.11 in full for both units: neither face prints anything that ignores an
                    // obstacle, so neither one does.
                    EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(CoMoveActorAtOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(CoMoveFriendAtOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(CoMoveDistanceOutput, EffectValueKind.Integer)
                });

        /// <summary>The conventional effect identity for one of this block's Skill card texts.</summary>
        public static EffectDefinitionId CoMoveEffectId(CardDefinitionId cardId) =>
            new EffectDefinitionId(cardId.Value + "-skill");

        /// <summary>
        /// The five controls this block's two faces raise, in printed order: one per printed distance,
        /// two on This Way! and three on That Way!.
        ///
        /// <para>The words are the friend's, because the friend is who reads them - the bullet is the
        /// one half of the sentence they decide, and what it does to the board is that both of them
        /// travel that far. They name no direction, because the direction is not theirs.</para>
        ///
        /// <para>There is no confirm-or-decline anywhere in either face: every node is mandatory, and
        /// the modal's own decline does not exist for a <c>Choose</c> at a floor of one.
        /// <c>HanuCoMoveTests</c> derives the required set with
        /// <see cref="DecisionLabelRequirementQuery"/> rather than trusting this list.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> CoMoveDecisionLabels()
        {
            yield return DecisionArm(
                HanuCardContent.ThisWay, CoMoveOneSpaceNode, "Move both of us 1 space");
            yield return DecisionArm(
                HanuCardContent.ThisWay, CoMoveTwoSpacesNode, "Move both of us 2 spaces");
            yield return DecisionArm(
                HanuCardContent.ThatWay, CoMoveOneSpaceNode, "Move both of us 1 space");
            yield return DecisionArm(
                HanuCardContent.ThatWay, CoMoveTwoSpacesNode, "Move both of us 2 spaces");
            yield return DecisionArm(
                HanuCardContent.ThatWay, CoMoveThreeSpacesNode, "Move both of us 3 spaces");
        }
    }
}

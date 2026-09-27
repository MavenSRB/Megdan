using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.112d. Mrak's five Blue faces: three pushes that carry him after what they pushed,
    /// and two stomps that leave a rock where a unit stood.
    ///
    /// <para><b>The Boulder line</b> - Boulder Rush, Boulder Blitz, Boulderdozer: "Push a token, or an
    /// enemy unit, adjacent to you 1 or 2 spaces [1, 2 or 3 / 1, 2, 3 or 4], ignoring obstacles; you
    /// may move up to 2 [3 / 4] spaces in the direction of the push, ignoring obstacles." The subject
    /// is chosen first (<c>synthetic-choose-target@4</c> at distance one, on the "enemy unit or a
    /// token" side, naming the push as its consumer), so a face with nothing beside Mrak is not
    /// offered; the push is p.18's named Push with the distance Mrak's choice from one to the printed
    /// most, crossing obstacles to the first empty space it can reach and going as far as it can when
    /// none is reachable - zero included. The follow move is Mrak's own, optional, one straight line on
    /// the heading from him through the space the subject stood on, crossing obstacles and never ending
    /// on one (p.18 Ignoring Obstacles). That heading is fixed by the chosen subject, so it stands
    /// after a zero-space push too: the text sets no condition on the second clause.</para>
    ///
    /// <para><b>The stomps</b> - Stomping Step, Ground Shaker: "Move a unit in radius which is adjacent
    /// to terrain, or to a rock token, 1 space. Place a rock token in the space it occupied." The
    /// printed-Radius unit selection with 8.88's terrain and rock-kind adjacency names the one-space
    /// Move as its consumer, so a unit with nowhere to step is not offered and a face with no such
    /// unit is not offered at all; then p.18's short supply and the rock, into the space the unit left.
    /// Ground Shaker adds "May repeat once on a different target" - p.10's optional repeat excluding
    /// the unit the first instance moved. "A unit in radius" is somebody other than Mrak, the reading
    /// every printed-Radius unit selection in the build gives the phrase.</para>
    ///
    /// <para>Nothing here is registered until 8.112g deals him.</para>
    /// </summary>
    public static class MrakBlueFaces
    {
        public static CardDefinitionId BoulderRush { get; } = Id("boulder-rush");
        public static CardDefinitionId BoulderBlitz { get; } = Id("boulder-blitz");
        public static CardDefinitionId StompingStep { get; } = Id("stomping-step");
        public static CardDefinitionId Boulderdozer { get; } = Id("boulderdozer");
        public static CardDefinitionId GroundShaker { get; } = Id("ground-shaker");

        public const string StompText =
            "Move a unit in radius which is adjacent to terrain, or to a rock token, 1 space. Place a " +
            "rock token in the space it occupied.";

        public static IReadOnlyList<CardDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            Boulder(BoulderRush, "Boulder Rush", CardTier.I, 8, 6, 2, PrintedValueChannel.None,
                new[] { BoulderBlitz, StompingStep }),
            Boulder(BoulderBlitz, "Boulder Blitz", CardTier.II, 9, 7, 3, PrintedValueChannel.Defense,
                new[] { Boulderdozer, GroundShaker }),
            Stomp(StompingStep, "Stomping Step", CardTier.II, PrintedValueChannel.Attack,
                new[] { Boulderdozer, GroundShaker }, false),
            Boulder(Boulderdozer, "Boulderdozer", CardTier.III, 9, 7, 4, PrintedValueChannel.Movement,
                Array.Empty<CardDefinitionId>()),
            Stomp(GroundShaker, "Ground Shaker", CardTier.III, PrintedValueChannel.Attack,
                Array.Empty<CardDefinitionId>(), true)
        });

        /// <summary>The printed push/follow sentence, for a face pushing up to <paramref name="most"/>.</summary>
        public static string BoulderText(int most) =>
            "Push a token, or an enemy unit, adjacent to you " + Distances(most) +
            " spaces, ignoring obstacles; you may move up to " + most +
            " spaces in the direction of the push, ignoring obstacles.";

        private static string Distances(int most)
        {
            switch (most)
            {
                case 2: return "1 or 2";
                case 3: return "1, 2 or 3";
                case 4: return "1, 2, 3 or 4";
                default: throw new ArgumentOutOfRangeException(nameof(most), most, "No such face.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Faces
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Boulder(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int most,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MrakCardContent.PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        BoulderBody(card, most)),
                    Movement(2),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(BoulderText(most)));

        private static CardDefinition Stomp(
            CardDefinitionId card, string title, CardTier tier, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> next, bool repeats) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(9),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MrakCardContent.PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        StompBody(card, repeats)),
                    Movement(2),
                    Defense(7)
                },
                null, CardSourceContent.Transcribed(
                    repeats ? StompText + " May repeat once on a different target." : StompText));

        // -------------------------------------------------------------------------------------------
        // Bodies
        // -------------------------------------------------------------------------------------------

        public const string SubjectOutput = "chosen";
        public const string LineDestinationOutput = "destination";

        private static EffectDefinition BoulderBody(CardDefinitionId card, int most)
        {
            var root = new EffectNodeId("boulder-root");
            var subject = new EffectNodeId("boulder-subject");
            var push = new EffectNodeId("boulder-push");
            var actorSpace = new EffectNodeId("boulder-actor-space");
            var line = new EffectNodeId("boulder-follow-line");
            var follow = new EffectNodeId("boulder-follow-move");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MrakCardContent.ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, push, actorSpace, line, follow }),

                    // "a token, or an enemy unit, adjacent to you": distance one is card text, not an
                    // icon, and the side is p.19's "enemy unit or a token". The push is the consumer,
                    // so a piece the push could not take - an obstacle, an immune unit - is never
                    // offered; the immunity question is the push's own activity.
                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.ObjectPush),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSourceOrUnaligned),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(push))
                        },
                        outputs: new[] { new EffectOutputBinding(SubjectOutput, EffectValueKind.Coordinate) }),

                    // "1 or 2 spaces" is a distance Mrak chooses between the printed ends, which is
                    // the `up to` policy with the least printed distance as its floor (Kinetic
                    // Blast's shape); "ignoring obstacles" is the push's own crossing.
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", subject, SubjectOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(most)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(true)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.UnitOrToken)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding("moved", EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),

                    // "you may move up to N spaces in the direction of the push, ignoring
                    // obstacles": one of p.19's six files, the one from Mrak through where the subject
                    // stood. "May" is the policy, so the destination prompt carries its own Decline
                    // (a choice of one space with a floor of one has no empty answer to give), and an
                    // empty offer - the file blocked to its end - is a step not taken.
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("maximumDistance", EffectValue.Integer(most)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticRosterCapabilities.LineHeadingThroughParameter,
                                subject, SubjectOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(LineDestinationOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        follow, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(most)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, LineDestinationOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) })
                });
        }

        private static EffectDefinition StompBody(CardDefinitionId card, bool repeats)
        {
            var root = new EffectNodeId("stomp-root");
            var select = new EffectNodeId("stomp-select");
            var move = new EffectNodeId("stomp-move");
            var reclaim = new EffectNodeId("stomp-reclaim");
            var place = new EffectNodeId("stomp-place");
            var repeat = new EffectNodeId("stomp-repeat");
            var vacated = new[]
            {
                EffectInputBinding.FromNodeOutput(
                    MrakRockPlacementContent.SpaceParameterName, select,
                    SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                    EffectValueKind.Coordinate)
            };
            var children = new List<EffectNodeId> { select, move, reclaim, place };
            if (repeats) children.Add(repeat);

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),
                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AnyTeam),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.AdjacentToTerrainParameterName,
                            EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                            EffectValue.TokenKind(MrakComponentContent.RockId)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(move))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),

                // "1 space": p.11's exact distance, anywhere the unit may step.
                new EffectNodeDefinition(
                    move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", select, SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                            EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    MrakRockPlacementContent.ReclaimForSpaceV1, inputs: vacated),
                new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    MrakRockPlacementContent.PlaceInSpaceV1, inputs: vacated,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            MrakRockPlacementContent.PlacedCountOutput, EffectValueKind.Integer)
                    })
            };
            if (repeats)
            {
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentSelectionSubject,
                    boundSelectionNodeId: select));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MrakCardContent.ContentRevision,
                root, nodes);
        }

        private static ActionDefinition Movement(int value) => new ActionDefinition(
            MrakCardContent.SecondaryMovement, ActionKind.Movement,
            ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        private static ActionDefinition Defense(int value) => new ActionDefinition(
            MrakCardContent.SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        private static CardDefinitionId Id(string card) =>
            new CardDefinitionId(MrakCardContent.ContentSetId + "-" + card);
    }
}

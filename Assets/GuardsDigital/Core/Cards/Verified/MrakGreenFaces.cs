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
    /// Milestone 8.112e. Mrak's five Green faces: three that shift a unit toward the rocks and the
    /// terrain, and two that roll Mrak himself down a line of obstacles.
    ///
    /// <para><b>Treacherous Ground, Rockslide, Avalanche</b>: "You may move a unit in range 1 space
    /// to a space adjacent to terrain, or a rock token." The `may` wraps the sentence (the Rock Solid
    /// shape), so a face with no unit able to take such a step is not offered at all; the printed-Range
    /// unit selection names the Move as its consumer, so a unit with no qualifying space one step away
    /// is never offered either; the Move is p.11's exact one space,
    /// narrowed to destinations beside terrain or a rock by 8.88's own adjacency clauses. Avalanche
    /// adds "May repeat once" - p.10's repeat with no "different target" wording, so the same unit may
    /// be chosen again.</para>
    ///
    /// <para><b>Rolling Stone, Strolling Stone</b>: "Move any number of spaces in a straight line
    /// ignoring obstacles, without moving through more than one [two] empty space[s]. (The starting
    /// space and the destination space do not count.)" One straight line from Mrak, crossing
    /// obstacles, ending on an empty space, with at most one (two) empty spaces strictly between.
    /// "Any number" admits standing still (the settled Garrus reading, GA-Q1), which is the line's
    /// Decline.</para>
    ///
    /// <para>Nothing here is registered until 8.112g deals him.</para>
    /// </summary>
    public static class MrakGreenFaces
    {
        public static CardDefinitionId TreacherousGround { get; } = Id("treacherous-ground");
        public static CardDefinitionId Rockslide { get; } = Id("rockslide");
        public static CardDefinitionId RollingStone { get; } = Id("rolling-stone");
        public static CardDefinitionId Avalanche { get; } = Id("avalanche");
        public static CardDefinitionId StrollingStone { get; } = Id("strolling-stone");

        public const string ShiftText =
            "You may move a unit in range 1 space to a space adjacent to terrain, or a rock token.";

        public const string RollingText =
            "Move any number of spaces in a straight line ignoring obstacles, without moving through " +
            "more than one empty space. (The starting space and the destination space do not count.)";

        public const string StrollingText =
            "Move any number of spaces in a straight line ignoring obstacles, without moving through " +
            "more than two empty spaces.";

        /// <summary>
        /// How far "any number of spaces" may reach: beyond any board this build ships, so the line
        /// ends where the board or the empty-space budget does rather than at a printed number.
        /// </summary>
        public const int AnyNumberOfSpaces = 64;

        public static IReadOnlyList<CardDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            Shift(TreacherousGround, "Treacherous Ground", CardTier.I, 6, 4, 3,
                PrintedValueChannel.None, new[] { Rockslide, RollingStone }, false),
            Shift(Rockslide, "Rockslide", CardTier.II, 5, 4, 4, PrintedValueChannel.Attack,
                new[] { Avalanche, StrollingStone }, false),
            Roll(RollingStone, "Rolling Stone", CardTier.II, 4, 1,
                new[] { Avalanche, StrollingStone }, RollingText),
            Shift(Avalanche, "Avalanche", CardTier.III, 5, 5, 4, PrintedValueChannel.Radius,
                Array.Empty<CardDefinitionId>(), true),
            Roll(StrollingStone, "Strolling Stone", CardTier.III, 5, 2,
                Array.Empty<CardDefinitionId>(), StrollingText)
        });

        // -------------------------------------------------------------------------------------------
        // Faces
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Shift(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int range,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, bool repeats) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MrakCardContent.PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        ShiftBody(card, repeats)),
                    Movement(2),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(repeats ? ShiftText + " May repeat once." : ShiftText));

        private static CardDefinition Roll(
            CardDefinitionId card, string title, CardTier tier, int defense, int emptyBudget,
            IReadOnlyList<CardDefinitionId> next, string text) =>
            new CardDefinition(
                card, MrakCardContent.ContentRevision, title, new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, tier, PrintedValueChannel.Initiative, next),
                new[]
                {
                    new ActionDefinition(
                        MrakCardContent.PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        RollBody(card, emptyBudget)),
                    Movement(2),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        // -------------------------------------------------------------------------------------------
        // Bodies
        // -------------------------------------------------------------------------------------------

        private static EffectDefinition ShiftBody(CardDefinitionId card, bool repeats)
        {
            var root = new EffectNodeId("shift-root");
            var body = new EffectNodeId("shift-body");
            var select = new EffectNodeId("shift-select");
            var move = new EffectNodeId("shift-move");
            var repeat = new EffectNodeId("shift-repeat");
            var children = new List<EffectNodeId> { select, move };
            if (repeats) children.Add(repeat);

            var nodes = new List<EffectNodeDefinition>
            {
                // "You may move a unit in range": the `may` wraps the whole sentence, the Rock Solid
                // shape. The feasibility walk looks inside it, so with no unit that can take a
                // qualifying step the face can apply nothing and is not offered (6.5af); a selection
                // carrying the `may` itself would leave the Move undecidable and the face offered to
                // do nothing. Avalanche's repeat sits inside too, so it follows only a real move.
                new EffectNodeDefinition(
                    root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { body }, chooser: EffectChooserRole.Actor),
                new EffectNodeDefinition(
                    body, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),

                new EffectNodeDefinition(
                    select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeUnitTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.AnyTeam),
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

                // "1 space to a space adjacent to terrain, or a rock token": p.11's exact distance,
                // the destination narrowed by 8.88's own adjacency clauses.
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
                            DirectMoveDestinationPolicy.Unrestricted),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.DestinationAdjacentToTerrainParameterName,
                            EffectValue.Boolean(true)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.DestinationAdjacentToTokenKindParameterName,
                            EffectValue.TokenKind(MrakComponentContent.RockId))
                    },
                    outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) })
            };
            if (repeats)
            {
                // "May repeat once" - no "different target", so p.10's repeat "with the same range,
                // restrictions" leaves the same unit choosable.
                nodes.Add(new EffectNodeDefinition(
                    repeat, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MrakCardContent.ContentRevision,
                root, nodes);
        }

        public const string LineDestinationOutput = "destination";

        private static EffectDefinition RollBody(CardDefinitionId card, int emptyBudget)
        {
            var root = new EffectNodeId("roll-root");
            var actorSpace = new EffectNodeId("roll-actor-space");
            var line = new EffectNodeId("roll-line");
            var move = new EffectNodeId("roll-move");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MrakCardContent.ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { actorSpace, line, move }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),

                    // "Move any number of spaces in a straight line ignoring obstacles": one of the
                    // six files, crossing obstacles, ending on an empty space. "Any number" includes
                    // none (GA-Q1), which is this `may` line's Decline - the line offer's own floor
                    // is one space, because standing still is not a straight-line move.
                    new EffectNodeDefinition(
                        line, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                "maximumDistance", EffectValue.Integer(AnyNumberOfSpaces)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.IgnoresObstaclesParameter,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.LineMaximumEmptyCrossedParameter,
                                EffectValue.Integer(emptyBudget))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(LineDestinationOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "mover", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                "distance", EffectValue.Integer(AnyNumberOfSpaces)),
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

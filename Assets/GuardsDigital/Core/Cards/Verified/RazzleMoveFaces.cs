using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21f. Razzle's moves and swaps of his bodies - nine faces, composed on the vocabulary
    /// that already existed wherever it composes, with two new generic shapes
    /// (<see cref="SyntheticHeroBodyMoveCapabilities"/>) where it did not. Nothing here is registered
    /// until 9.21j deals him.
    ///
    /// <list type="bullet">
    /// <item><description><b>Alleyoop / Group Performance / Team Spirit</b> (Blue I-III, Skill - Ranged
    /// 3 / 4 / 4): "Swap places with a friendly hero in range. Move one other you 1 space" / "up to 2
    /// spaces" / "up to 3 spaces". The printed-Range hero selection on his own side names p.19's Swap
    /// with the actor as its consumer; then one other body (RZ-Q8: any, anywhere) is moved by
    /// card-text direct Move. With no other body the Swap still resolves and the move is not
    /// performed (RZ-Q8, p.10), so the move clause is `if able` and the offer is judged on the
    /// Swap.</description></item>
    /// <item><description><b>Magic Trick / Aaaand it's gone!</b> (Blue II / III, Skill): "Push a unit
    /// adjacent to you up to 2 (3) spaces; for every space the target moved, move this one of you one
    /// space in the opposite direction." Min's Crane Stance push on any side, then the acting body
    /// retreats along the push's line, away from where the target stood, exactly as many spaces as
    /// the target actually moved - 0 after a 0 push - and stops where the line is blocked
    /// (RZ-Q10).</description></item>
    /// <item><description><b>Tightrope / High Wire / Wire Dancers</b> (Green I-III, Movement 2): "After
    /// you move you may move another one of you 1 space" / "up to 2" / "up to 3". p.11's "after
    /// movement" text (Ursafar's moment), so it applies after a Movement of 0 spaces and never after
    /// a Fast Travel, which replaces the Movement (RZ-Q9); the other body is any, anywhere.</description></item>
    /// <item><description><b>Theatrics</b> (Green II, Skill - Ranged 2): "Swap places with a minion in
    /// range." Arien's Arcane Whirlpool on any side.</description></item>
    /// </list>
    ///
    /// <para><b>His own bodies are never the friendly hero or the pushed unit (RZ-Q5).</b> The
    /// printed-icon and roster selections leave out every body of the actor at hero grain, and p.19's
    /// Swap refuses to exchange a piece with itself.</para>
    /// </summary>
    public static partial class RazzleCardContent
    {
        public static CardDefinitionId Alleyoop { get; } = Id("alleyoop");
        public static CardDefinitionId GroupPerformance { get; } = Id("group-performance");
        public static CardDefinitionId MagicTrick { get; } = Id("magic-trick");
        public static CardDefinitionId TeamSpirit { get; } = Id("team-spirit");
        public static CardDefinitionId AaaandItsGone { get; } = Id("aaaand-its-gone");
        public static CardDefinitionId Tightrope { get; } = Id("tightrope");
        public static CardDefinitionId HighWire { get; } = Id("high-wire");
        public static CardDefinitionId Theatrics { get; } = Id("theatrics");
        public static CardDefinitionId WireDancers { get; } = Id("wire-dancers");

        /// <summary>The Green Movement faces' printed primary: "Movement" with the value 2.</summary>
        public static ActionDefinitionId PrimaryMovement { get; } = new ActionDefinitionId("primary-movement");

        /// <summary>
        /// 9.21f's block list (read by the corpus sweeps until 9.21j mints the aggregate): the nine
        /// moves and swaps of his bodies.
        /// </summary>
        public static IReadOnlyList<CardDefinition> MoveDefinitions =>
            new[]
            {
                AlleyoopDefinition, GroupPerformanceDefinition, MagicTrickDefinition,
                TeamSpiritDefinition, AaaandItsGoneDefinition,
                TightropeDefinition, HighWireDefinition, TheatricsDefinition, WireDancersDefinition
            };

        public const string AlleyoopText =
            "Swap places with a friendly hero in range. Move one other you 1 space.";

        public const string GroupPerformanceText =
            "Swap places with a friendly hero in range. Move one other you up to 2 spaces.";

        public const string TeamSpiritText =
            "Swap places with a friendly hero in range. Move one other you up to 3 spaces.";

        public const string MagicTrickText =
            "Push a unit adjacent to you up to 2 spaces; for every space the target moved, move this " +
            "one of you one space in the opposite direction.";

        public const string AaaandItsGoneText =
            "Push a unit adjacent to you up to 3 spaces; for every space the target moved, move this " +
            "one of you one space in the opposite direction.";

        public const string TightropeText = "After you move you may move another one of you 1 space.";

        public const string HighWireText = "After you move you may move another one of you up to 2 spaces.";

        public const string WireDancersText = "After you move you may move another one of you up to 3 spaces.";

        public const string TheatricsText = "Swap places with a minion in range.";

        /// <summary>Blue, Tier I: Initiative 10, Defense 2, Movement 3, Skill - Ranged, Range 3.</summary>
        public static CardDefinition AlleyoopDefinition => LazyAlleyoop.Value;

        private static readonly Lazy<CardDefinition> LazyAlleyoop = new Lazy<CardDefinition>(() =>
            BlueSkill(Alleyoop, "Alleyoop", CardTier.I, 10, 2, 3, PrintedValueChannel.None,
                new[] { GroupPerformance, MagicTrick }, 3,
                RazzleEffectContent.SwapThenMoveOther(Alleyoop, ContentRevision, 1, false), AlleyoopText));

        /// <summary>Blue, Tier II: Initiative 11, Defense 3, Movement 3, Range 4, Attack item.</summary>
        public static CardDefinition GroupPerformanceDefinition => LazyGroupPerformance.Value;

        private static readonly Lazy<CardDefinition> LazyGroupPerformance = new Lazy<CardDefinition>(() =>
            BlueSkill(GroupPerformance, "Group Performance", CardTier.II, 11, 3, 3, PrintedValueChannel.Attack,
                new[] { TeamSpirit, AaaandItsGone }, 4,
                RazzleEffectContent.SwapThenMoveOther(GroupPerformance, ContentRevision, 2, true),
                GroupPerformanceText));

        /// <summary>Blue, Tier II: Initiative 11, Defense 3, Movement 3, Skill (no icon), Defense item.</summary>
        public static CardDefinition MagicTrickDefinition => LazyMagicTrick.Value;

        private static readonly Lazy<CardDefinition> LazyMagicTrick = new Lazy<CardDefinition>(() =>
            BlueSkill(MagicTrick, "Magic Trick", CardTier.II, 11, 3, 3, PrintedValueChannel.Defense,
                new[] { TeamSpirit, AaaandItsGone }, 0,
                RazzleEffectContent.PushAndRetreat(MagicTrick, ContentRevision, 2), MagicTrickText));

        /// <summary>Blue, Tier III: Initiative 11, Defense 3, Movement 3, Range 4, Radius item.</summary>
        public static CardDefinition TeamSpiritDefinition => LazyTeamSpirit.Value;

        private static readonly Lazy<CardDefinition> LazyTeamSpirit = new Lazy<CardDefinition>(() =>
            BlueSkill(TeamSpirit, "Team Spirit", CardTier.III, 11, 3, 3, PrintedValueChannel.Radius,
                Array.Empty<CardDefinitionId>(), 4,
                RazzleEffectContent.SwapThenMoveOther(TeamSpirit, ContentRevision, 3, true), TeamSpiritText));

        /// <summary>Blue, Tier III: Initiative 11, Defense 3, Movement 3, Skill (no icon), Defense item.</summary>
        public static CardDefinition AaaandItsGoneDefinition => LazyAaaandItsGone.Value;

        private static readonly Lazy<CardDefinition> LazyAaaandItsGone = new Lazy<CardDefinition>(() =>
            BlueSkill(AaaandItsGone, "Aaaand it's gone!", CardTier.III, 11, 3, 3, PrintedValueChannel.Defense,
                Array.Empty<CardDefinitionId>(), 0,
                RazzleEffectContent.PushAndRetreat(AaaandItsGone, ContentRevision, 3), AaaandItsGoneText));

        /// <summary>Green, Tier I: Initiative 2, Defense 1, Movement 2 (the primary).</summary>
        public static CardDefinition TightropeDefinition => LazyTightrope.Value;

        private static readonly Lazy<CardDefinition> LazyTightrope = new Lazy<CardDefinition>(() =>
            GreenMovement(Tightrope, "Tightrope", CardTier.I, 2, 1, PrintedValueChannel.None,
                new[] { HighWire, Theatrics }, 1, false, TightropeText));

        /// <summary>Green, Tier II: Initiative 2, Defense 2, Movement 2 (the primary), Initiative item.</summary>
        public static CardDefinition HighWireDefinition => LazyHighWire.Value;

        private static readonly Lazy<CardDefinition> LazyHighWire = new Lazy<CardDefinition>(() =>
            GreenMovement(HighWire, "High Wire", CardTier.II, 2, 2, PrintedValueChannel.Initiative,
                new[] { WireDancers, Id("spectacle") }, 2, true, HighWireText));

        /// <summary>Green, Tier III: Initiative 1, Defense 2, Movement 2 (the primary), Initiative item.</summary>
        public static CardDefinition WireDancersDefinition => LazyWireDancers.Value;

        private static readonly Lazy<CardDefinition> LazyWireDancers = new Lazy<CardDefinition>(() =>
            GreenMovement(WireDancers, "Wire Dancers", CardTier.III, 1, 2, PrintedValueChannel.Initiative,
                Array.Empty<CardDefinitionId>(), 3, true, WireDancersText));

        /// <summary>Green, Tier II: Initiative 2, Defense 2, Movement 2, Skill - Ranged, Range 2, Attack item.</summary>
        public static CardDefinition TheatricsDefinition => LazyTheatrics.Value;

        private static readonly Lazy<CardDefinition> LazyTheatrics = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                Theatrics, ContentRevision, "Theatrics", new InitiativeValue(2),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    new[] { WireDancers, Id("spectacle") }),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        RazzleEffectContent.SwapWithAMinion(Theatrics, ContentRevision)),
                    Movement(2),
                    Defense(2)
                },
                null, CardSourceContent.Transcribed(TheatricsText)));

        /// <summary>
        /// The five Blue faces' one printed shape: a Skill primary (a Range icon where the face prints
        /// "Skill - Ranged", none where it prints "Skill"), Movement 3 and the printed Defense.
        /// </summary>
        private static CardDefinition BlueSkill(
            CardDefinitionId card, string name, CardTier tier, int initiative, int defense, int movement,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, int range,
            EffectDefinition text, string printed) =>
            new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        range > 0
                            ? new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) }
                            : Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        text),
                    Movement(movement),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(printed));

        /// <summary>
        /// The three Green Movement faces' one printed shape: the Movement 2 primary whose text p.11
        /// applies after the walk ("After you move"), and the printed Defense. They print no secondary
        /// Movement: the Movement is the primary.
        /// </summary>
        private static CardDefinition GreenMovement(
            CardDefinitionId card, string name, CardTier tier, int initiative, int defense,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, int spaces, bool upTo,
            string printed) =>
            new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        RazzleEffectContent.MoveAnotherAfterMovement(card, ContentRevision, spaces, upTo),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheMovement),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(printed));
    }

    public static partial class RazzleEffectContent
    {
        /// <summary>
        /// Milestone 9.21f. "Another one of you" / "one other you": one standing body other than the
        /// acting body, anywhere on the board, published as its space for the Move that follows.
        /// </summary>
        public static EffectCapabilityReference OtherBodyV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-other-body"), 1);

        /// <summary>
        /// Milestone 9.21f. "Move this one of you one space in the opposite direction" for every space
        /// the target moved: where the acting body's straight travel away from the pushed target ends.
        /// </summary>
        public static EffectCapabilityReference RetreatLineEndV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-retreat-line-end"), 1);

        /// <summary>9.21f's handlers, appended to <see cref="Handlers"/> in one place.</summary>
        public static IEnumerable<IEffectCapabilityHandler> MoveHandlers() =>
            new[]
            {
                SyntheticHeroBodyMoveCapabilities.ChooseOtherActorBodyHandler(OtherBodyV1),
                SyntheticHeroBodyMoveCapabilities.ContinuedLineEndHandler(RetreatLineEndV1)
            };

        /// <summary>The selection of a face's other body, so a paused selection names its face.</summary>
        public static string OtherBodyNode(CardDefinitionId card) => card.Value + "-other-body";

        /// <summary>The Move of that other body.</summary>
        public static string OtherBodyMoveNode(CardDefinitionId card) => card.Value + "-other-body-move";

        /// <summary>The friendly hero or minion a Swap face names.</summary>
        public static string SwapSubjectNode(CardDefinitionId card) => card.Value + "-swap-subject";

        /// <summary>The Swap with the acting body.</summary>
        public static string SwapNode(CardDefinitionId card) => card.Value + "-swap";

        /// <summary>The unit a Push face names.</summary>
        public static string PushSubjectNode(CardDefinitionId card) => card.Value + "-push-subject";

        /// <summary>The Push, which asks its distance.</summary>
        public static string PushNode(CardDefinitionId card) => card.Value + "-push";

        /// <summary>
        /// Alleyoop, Group Performance and Team Spirit: "Swap places with a friendly hero in range. Move
        /// one other you 1 space [up to 2 / up to 3 spaces]."
        ///
        /// <para><b>The Swap.</b> The printed-Range hero selection (the icon with p.16's items, R1) on
        /// his own side, the Skill's activity, naming p.19's Swap with the actor as its consumer, so a
        /// hero the Swap could not take is never offered (8.35l). It never offers a body of his (RZ-Q5):
        /// the selection leaves out the actor at hero grain, and the Swap refuses a piece with itself.
        /// The Swap moves the ACTING body (RZ-Q4).</para>
        ///
        /// <para><b>"Move one other you".</b> Any other body, anywhere - the Range bounds only the Swap
        /// (RZ-Q8) - chosen by <see cref="OtherBodyV1"/>, which names the Move as its consumer, then
        /// card-text direct Move of that body, unrestricted in direction. "1 space" is exact and "up
        /// to N" admits 0 (W5). The pair is <c>if able</c>: with no other body standing the Swap has
        /// still resolved and the move is simply not performed (RZ-Q8, p.10), and the offer is judged
        /// on the Swap.</para>
        /// </summary>
        public static EffectDefinition SwapThenMoveOther(
            CardDefinitionId card, string contentRevision, int spaces, bool upTo)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var friend = new EffectNodeId(SwapSubjectNode(card));
            var swap = new EffectNodeId(SwapNode(card));
            var other = new EffectNodeId(OtherBodyNode(card));
            var move = new EffectNodeId(OtherBodyMoveNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { friend, swap, other, move }),
                    new EffectNodeDefinition(
                        friend, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            // A Skill; there is no attacker to leave out.
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    SwapWithTheActor(swap, friend, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName),
                    OtherBody(other, EffectFailurePolicy.IfAble, move),
                    MoveOtherBody(move, other, spaces, upTo)
                });
        }

        /// <summary>
        /// Tightrope, High Wire and Wire Dancers: "After you move you may move another one of you 1
        /// space [up to 2 / up to 3 spaces]." The Movement primary's text at p.11's "after movement"
        /// moment (<see cref="Cards.CardTextTiming.AfterTheMovement"/>): it runs when the walk finishes,
        /// a walk of 0 spaces included (p.11 "applied even if you choose to move zero spaces"), and
        /// never after a Fast Travel, which replaces the Movement rather than performing it (RZ-Q9).
        /// "You may" is the selection's own decline; with no other body standing nothing is asked.
        /// </summary>
        public static EffectDefinition MoveAnotherAfterMovement(
            CardDefinitionId card, string contentRevision, int spaces, bool upTo)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var other = new EffectNodeId(OtherBodyNode(card));
            var move = new EffectNodeId(OtherBodyMoveNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-movement-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { other, move }),
                    OtherBody(other, EffectFailurePolicy.May, move),
                    MoveOtherBody(move, other, spaces, upTo)
                });
        }

        /// <summary>
        /// Magic Trick and Aaaand it's gone!: "Push a unit adjacent to you up to 2 [3] spaces; for every
        /// space the target moved, move this one of you one space in the opposite direction."
        ///
        /// <para><b>The Push</b> is Min's Crane Stance shape on either side: a mandatory roster
        /// selection of one unit adjacent to the acting body ("a unit" - any side; never a body of his,
        /// RZ-Q5, because the selection leaves the actor out at hero grain), asked as p.18's push
        /// activity, then p.18's named Push whose DISTANCE "up to" makes optional (W5, W8: a target it
        /// would move 0 spaces is still a target).</para>
        ///
        /// <para><b>The retreat (RZ-Q10).</b> "Every space the target moved" is the Push's realised
        /// travel (<see cref="SyntheticDisplacementCapabilities.PushedSpacesOutputName"/>), not the
        /// distance chosen. "The opposite direction" is the line from where the target stood through
        /// the acting body ("this one of you"), continued - the heading the Push already fixed.
        /// <see cref="RetreatLineEndV1"/> names where that travel ends, stopping before a block, and
        /// card-text direct Move walks the acting body there. The step is mandatory in print and has
        /// nothing to do after a 0-space push or a line blocked at once, which p.10 skips - so both
        /// nodes are <c>if able</c>.</para>
        /// </summary>
        public static EffectDefinition PushAndRetreat(CardDefinitionId card, string contentRevision, int most)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var subject = new EffectNodeId(PushSubjectNode(card));
            var push = new EffectNodeId(PushNode(card));
            var actorSpace = new EffectNodeId(card.Value + "-actor-space");
            var end = new EffectNodeId(card.Value + "-retreat-end");
            var retreat = new EffectNodeId(card.Value + "-retreat");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, push, actorSpace, end, retreat }),
                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Unit),
                            // "a unit": p.19's either side.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.AnyTeam),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.ObjectPush)
                        },
                        // All three: the selection publishes the unit it named whether or not a later
                        // node reads it, and an undeclared recorded outcome is a snapshot this build
                        // refuses to read back (found restoring a paused push at 9.21f).
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        push, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.PushTargetV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "target", subject, SyntheticRosterCapabilities.ChosenSpaceOutput,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(most)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                PushEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("distance", EffectValueKind.Integer),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.PushedSpacesOutputName,
                                EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        end, EffectNodeKind.Capability, EffectFailurePolicy.IfAble, RetreatLineEndV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticHeroBodyMoveCapabilities.ContinuedLineFromParameter, subject,
                                SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticHeroBodyMoveCapabilities.DistanceParameter, push,
                                SyntheticDisplacementCapabilities.PushedSpacesOutputName,
                                EffectValueKind.Integer)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticHeroBodyMoveCapabilities.DestinationOutput, EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        retreat, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("mover", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput(
                                "distance", push, SyntheticDisplacementCapabilities.PushedSpacesOutputName,
                                EffectValueKind.Integer),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName, end,
                                SyntheticHeroBodyMoveCapabilities.DestinationOutput, EffectValueKind.Coordinate)
                        },
                        outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) })
                });
        }

        /// <summary>
        /// Theatrics: "Swap places with a minion in range." Arien's Arcane Whirlpool on either side: the
        /// printed-Range minion selection naming the Swap with the actor as its consumer (8.35l), and
        /// p.19's Swap of the acting body with that minion.
        /// </summary>
        public static EffectDefinition SwapWithAMinion(CardDefinitionId card, string contentRevision)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var minion = new EffectNodeId(SwapSubjectNode(card));
            var swap = new EffectNodeId(SwapNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { minion, swap }),
                    new EffectNodeDefinition(
                        minion, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeMinionTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            // "a minion": either side (p.19 Swap).
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AnyTeam),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    SwapWithTheActor(swap, minion, SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName)
                });
        }

        private static EffectNodeDefinition SwapWithTheActor(
            EffectNodeId swap, EffectNodeId subject, string subjectSpaceOutput) =>
            new EffectNodeDefinition(
                swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapWithActorV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", subject, subjectSpaceOutput, EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.Unit)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                });

        private static EffectNodeDefinition OtherBody(
            EffectNodeId other, EffectFailurePolicy policy, EffectNodeId move) =>
            new EffectNodeDefinition(
                other, EffectNodeKind.Capability, policy, OtherBodyV1,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(move))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticHeroBodyMoveCapabilities.OtherBodyOutput, EffectValueKind.Coordinate)
                });

        /// <summary>
        /// Card-text direct Move of the chosen other body (p.11: never a Fast Travel). "1 space" is an
        /// exact distance, performed if able - an unanswered selection leaves no body to move; "up to
        /// N" admits 0 (W5).
        /// </summary>
        private static EffectNodeDefinition MoveOtherBody(
            EffectNodeId move, EffectNodeId other, int spaces, bool upTo) =>
            new EffectNodeDefinition(
                move, EffectNodeKind.Capability, upTo ? EffectFailurePolicy.UpTo : EffectFailurePolicy.IfAble,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "mover", other, SyntheticHeroBodyMoveCapabilities.OtherBodyOutput,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(spaces)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) });
    }
}

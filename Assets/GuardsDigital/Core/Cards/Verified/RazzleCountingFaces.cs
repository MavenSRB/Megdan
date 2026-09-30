using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21g. Razzle's counting faces - the Red Attacks Rummage and Ransack, and Crowd
    /// Control's Defense text - each counting "other you in radius": the bodies of his other than the
    /// one performing (or, for a Defense, the attacked one, RZ-Q3), within the card's printed Radius 3
    /// raised by Radius items (R1). Registered and dealt at 9.21j.
    /// </summary>
    public static partial class RazzleCardContent
    {
        public static CardDefinitionId Rummage { get; } = Id("rummage");
        public static CardDefinitionId Ransack { get; } = Id("ransack");


        public const string RummageText =
            "Target a unit adjacent to you. After the attack: If there's another you in radius, you may " +
            "retrieve a discarded card. (All of you share cards in hand, deck, and discard).";

        public const string RansackText =
            "Target a unit adjacent to you. After the attack: For every other you in radius you may " +
            "retrieve a discarded card.";

        /// <summary>
        /// Crowd Control's Defense half: "When used as a defense action, +2 [Defense] Defense for each
        /// other you in radius." p.14's printed <c>1+</c> - a value its own text raises - as the typed
        /// declaration that raises it: +2 per other body within the Defense primary's printed Radius 3
        /// (with Radius items, R1) of the ATTACKED body, which "other you" excludes (RZ-Q3). Read at
        /// the Defense response and in the Defense preview through <see cref="DefenseValueQuery"/>,
        /// which both hand the attacked body.
        /// </summary>
        public static IReadOnlyList<RosterValueContributionDefinition> CrowdControlDefenseText =>
            new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense,
                    RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius,
                    CrowdControlDefensePerOtherBody)
            };

        /// <summary>"+2 Defense for each other you in radius".</summary>
        public const int CrowdControlDefensePerOtherBody = 2;

        /// <summary>Red, Tier II: Initiative 9, Defense 4, Movement 4, Attack 4, Radius 3, Initiative item.</summary>
        public static CardDefinition RummageDefinition => LazyRummage.Value;

        private static readonly Lazy<CardDefinition> LazyRummage = new Lazy<CardDefinition>(() =>
            CountingAttack(Rummage, "Rummage", CardTier.II, 9, 4, PrintedValueChannel.Initiative,
                new[] { IntoThinAir, Ransack },
                RazzleEffectContent.RummageAfterTheAttack(Rummage, ContentRevision),
                RummageText));

        /// <summary>Red, Tier III: Initiative 10, Defense 4, Movement 4, Attack 5, Radius 3, Movement item.</summary>
        public static CardDefinition RansackDefinition => LazyRansack.Value;

        private static readonly Lazy<CardDefinition> LazyRansack = new Lazy<CardDefinition>(() =>
            CountingAttack(Ransack, "Ransack", CardTier.III, 10, 5, PrintedValueChannel.Movement,
                Array.Empty<CardDefinitionId>(),
                RazzleEffectContent.RansackAfterTheAttack(Ransack, ContentRevision),
                RansackText));

        /// <summary>
        /// The two faces' one printed shape: p.13's non-Ranged Attack on a unit adjacent to the acting
        /// body whose Radius 3 icon bounds only the text's "in radius" (p.12: "Non-Ranged Attacks can
        /// only target units adjacent to you (even if the card has a radius icon)"), then the
        /// after-the-attack retrieval, Movement 4 and Defense 4.
        /// </summary>
        private static CardDefinition CountingAttack(
            CardDefinitionId card, string name, CardTier tier, int initiative, int attack,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, EffectDefinition text,
            string printed) =>
            new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 3)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        text,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                    Movement(4),
                    Defense(4)
                },
                null, CardSourceContent.Transcribed(printed));
    }

    public static partial class RazzleEffectContent
    {
        /// <summary>
        /// Milestone 9.21g. "Other you in radius": how many bodies of the actor other than the acting
        /// one stand within the action's printed Radius (with items) of the acting body.
        /// </summary>
        public static EffectCapabilityReference OtherBodiesInRadiusV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-other-bodies-in-radius"), 1);

        /// <summary>
        /// The most retrievals Ransack's "For every other you in radius" can count: RZ-Q1 (a) gives
        /// him four bodies in all, so at most three are "other". One gated optional retrieval per
        /// possible other body, as Stunt Doubles composes one optional spawn per printed body.
        /// </summary>
        public const int RansackMostOtherBodies = 3;

        /// <summary>Milestone 9.21g's handlers, appended once to <see cref="Handlers"/>.</summary>
        internal static IEnumerable<IEffectCapabilityHandler> CountingHandlers() =>
            new[] { SyntheticHeroBodyCountCapabilities.ActorOtherBodiesInRadiusHandler(OtherBodiesInRadiusV1) };

        /// <summary>The n-th "other you in radius" gate of a counting face (1-based).</summary>
        public static string CountGateNode(CardDefinitionId card, int index) => $"{card.Value}-gate-{index}";

        /// <summary>The n-th optional retrieval ("you may") of a counting face (1-based).</summary>
        public static string MayRetrieveNode(CardDefinitionId card, int index) => $"{card.Value}-may-retrieve-{index}";

        /// <summary>The n-th retrieval step itself (1-based).</summary>
        public static string RetrieveNode(CardDefinitionId card, int index) => $"{card.Value}-retrieve-{index}";

        /// <summary>
        /// Rummage: "After the attack: If there's another you in radius, you may retrieve a discarded
        /// card. (All of you share cards in hand, deck, and discard)." A Conditional over the printed
        /// "If" (at least one other body in radius) with no else-child - a false gate is RULING 53's
        /// legal do-nothing - whose then-child is the optional retrieval Snorri's, Mrak's and Wuk's
        /// "you may retrieve a discarded card" compose. The reminder composes to no node: one hero,
        /// one discard (the many-bodies model).
        /// </summary>
        public static EffectDefinition RummageAfterTheAttack(CardDefinitionId card, string contentRevision) =>
            GatedRetrievals(card, contentRevision, 1);

        /// <summary>
        /// Ransack: "After the attack: For every other you in radius you may retrieve a discarded card."
        /// One optional retrieval per other body in radius, each its own "may": a Sequence of
        /// <see cref="RansackMostOtherBodies"/> Conditionals, the n-th true when at least n other
        /// bodies stand in radius of the acting body. A retrieval moves no body, so each gate reads the
        /// count the step started with.
        /// </summary>
        public static EffectDefinition RansackAfterTheAttack(CardDefinitionId card, string contentRevision) =>
            GatedRetrievals(card, contentRevision, RansackMostOtherBodies);

        private static EffectDefinition GatedRetrievals(CardDefinitionId card, string contentRevision, int gates)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var nodes = new List<EffectNodeDefinition>();
            var children = new List<EffectNodeId>();
            for (var index = 1; index <= gates; index++)
            {
                var gate = new EffectNodeId(CountGateNode(card, index));
                var may = new EffectNodeId(MayRetrieveNode(card, index));
                var retrieve = new EffectNodeId(RetrieveNode(card, index));
                children.Add(gate);
                nodes.Add(new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory, OtherBodiesInRadiusV1,
                    childNodeIds: new[] { may },
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            SyntheticHeroBodyCountCapabilities.AtLeastParameter, EffectValue.Integer(index))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticHeroBodyCountCapabilities.CountOutput, EffectValueKind.Integer),
                        new EffectOutputBinding(
                            SyntheticHeroBodyCountCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    may, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { retrieve }, chooser: EffectChooserRole.Actor));
                nodes.Add(new EffectNodeDefinition(
                    retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                    },
                    outputs: new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: children));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), contentRevision, root, nodes);
        }
    }
}

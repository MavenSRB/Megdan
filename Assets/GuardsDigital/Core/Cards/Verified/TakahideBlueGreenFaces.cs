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
    /// Milestone 9.32e. Takahide's five Blue Skills (Radius) and five Green Ranged Skills, each a
    /// sentence about "a friendly hero" - never Takahide himself (p.10 Picking Targets) - whom he
    /// chooses (p.10 Making Choices). With no such teammate in reach the face is greyed, Hold and the
    /// secondary actions staying available (TK-Q4, Rule 14).
    ///
    /// <list type="bullet">
    /// <item>Proven Warrior, Chosen Champion: "Choose a card in the discard of a friendly hero in radius.
    /// An enemy hero in radius discards a card of the same color, if able." Only a teammate with a
    /// choosable card is offered (6.8l; a facedown card has no colour). The second sentence is Gydion's
    /// Cloud Kill's shape: Takahide picks the enemy hero, who discards "if able".</item>
    /// <item>The Right Hand: "... Up to two enemy heroes in radius discard ..." - W3: every such hero
    /// present is hit, up to two, Takahide choosing which when more stand there.</item>
    /// <item>Calculated Risk, Tactical Gambit: "A friendly hero in radius may discard an attack card. If
    /// that hero has a card in the discard, that hero may move up to 2 spaces[, ignoring obstacles]."
    /// The discard and the move are the teammate's (RO-Q2); the check reads the pile after the
    /// optional discard, so a teammate who declines but already has a discarded card still moves.</item>
    /// <item>Come to Aid, Bring the Relief, Commit Reserves: the same check gating Takahide's own "you
    /// may move up to 3 [4] spaces[, ignoring obstacles]".</item>
    /// <item>Pledge of Allegiance, Loyal Retainer: the same check gating "both you and that hero gain 1
    /// coin [2 coins] and you may retrieve a discarded card" - his own card (p.10).</item>
    /// </list>
    ///
    /// <para>Every friendly hero in reach is offered by the may-discard faces: the discard is theirs to
    /// decline and the check reads the pile whatever they decided, so no teammate there is one the
    /// sentence cannot act on.</para>
    /// </summary>
    public static partial class TakahideCardContent
    {
        public const string ProvenWarriorText =
            "Choose a card in the discard of a friendly hero in radius. An enemy hero in radius discards " +
            "a card of the same color, if able.";

        public const string ChosenChampionText = ProvenWarriorText;

        public const string TheRightHandText =
            "Choose a card in the discard of a friendly hero in radius. Up to two enemy heroes in radius " +
            "discard a card of the same color, if able.";

        public const string CalculatedRiskText =
            "A friendly hero in radius may discard an attack card. If that hero has a card in the " +
            "discard, that hero may move up to 2 spaces.";

        public const string TacticalGambitText =
            "A friendly hero in radius may discard an attack card. If that hero has a card in the " +
            "discard, that hero may move up to 2 spaces, ignoring obstacles.";

        public const string ComeToAidText =
            "A friendly hero in range may discard a card. If that hero has a card in the discard you may " +
            "move up to 3 spaces.";

        public const string BringTheReliefText =
            "A friendly hero in range may discard a card. If that hero has a card in the discard you may " +
            "move up to 4 spaces.";

        public const string CommitReservesText =
            "A friendly hero in range may discard a card. If that hero has a card in the discard you may " +
            "move up to 4 spaces, ignoring obstacles.";

        public const string PledgeOfAllegianceText =
            "A friendly hero in range may discard a card. If that hero has a card in the discard, both " +
            "you and that hero gain 1 coin and you may retrieve a discarded card.";

        public const string LoyalRetainerText =
            "A friendly hero in range may discard a card. If that hero has a card in the discard, both " +
            "you and that hero gain 2 coins and you may retrieve a discarded card.";

        /// <summary>Milestone 9.32e's faces: the five Blues and the five Greens.</summary>
        public static IReadOnlyList<CardDefinition> BlueAndGreenDefinitions => LazyBlueAndGreen.Value;

        /// <summary>The handler 9.32e's faces need beyond the build's catalog. Registered with the dealing (9.32h).</summary>
        public static IEnumerable<IEffectCapabilityHandler> BlueAndGreenHandlers() =>
            SyntheticDiscardedCardChoiceCapabilities.Handlers();

        /// <summary>Blue, Tier I: Initiative 9, Defense 4, Movement 2, Skill, Radius 3.</summary>
        public static CardDefinition ProvenWarriorDefinition { get; } = BlueSkill(
            ProvenWarrior, "Proven Warrior", CardTier.I, 9, defense: 4, radius: 3, PrintedValueChannel.None,
            new[] { ChosenChampion, CalculatedRisk }, SameColorDiscard(ProvenWarrior, enemies: 1),
            ProvenWarriorText);

        /// <summary>Blue, Tier II: Initiative 10, Defense 4, Movement 2, Skill, Radius 4; Attack item.</summary>
        public static CardDefinition ChosenChampionDefinition { get; } = BlueSkill(
            ChosenChampion, "Chosen Champion", CardTier.II, 10, defense: 4, radius: 4, PrintedValueChannel.Attack,
            new[] { TheRightHand, TacticalGambit }, SameColorDiscard(ChosenChampion, enemies: 1),
            ChosenChampionText);

        /// <summary>Blue, Tier II: Initiative 10, Defense 4, Movement 2, Skill, Radius 4; Initiative item.</summary>
        public static CardDefinition CalculatedRiskDefinition { get; } = BlueSkill(
            CalculatedRisk, "Calculated Risk", CardTier.II, 10, defense: 4, radius: 4,
            PrintedValueChannel.Initiative, new[] { TheRightHand, TacticalGambit },
            FriendDiscardThen(CalculatedRisk, SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                attackCardsOnly: true, FriendMayMove(CalculatedRisk, ignoresObstacles: false)),
            CalculatedRiskText);

        /// <summary>Blue, Tier III: Initiative 10, Defense 5, Movement 2, Skill, Radius 4; Attack item.</summary>
        public static CardDefinition TheRightHandDefinition { get; } = BlueSkill(
            TheRightHand, "The Right Hand", CardTier.III, 10, defense: 5, radius: 4, PrintedValueChannel.Attack,
            Array.Empty<CardDefinitionId>(), SameColorDiscard(TheRightHand, enemies: 2), TheRightHandText);

        /// <summary>Blue, Tier III: Initiative 10, Defense 5, Movement 2, Skill, Radius 4; Initiative item.</summary>
        public static CardDefinition TacticalGambitDefinition { get; } = BlueSkill(
            TacticalGambit, "Tactical Gambit", CardTier.III, 10, defense: 5, radius: 4,
            PrintedValueChannel.Initiative, Array.Empty<CardDefinitionId>(),
            FriendDiscardThen(TacticalGambit, SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                attackCardsOnly: true, FriendMayMove(TacticalGambit, ignoresObstacles: true)),
            TacticalGambitText);

        /// <summary>Green, Tier I: Initiative 4, Defense 3, Movement 2, Skill - Ranged, Range 3.</summary>
        public static CardDefinition ComeToAidDefinition { get; } = GreenSkill(
            ComeToAid, "Come to Aid", CardTier.I, 4, defense: 3, range: 3, PrintedValueChannel.None,
            new[] { BringTheRelief, PledgeOfAllegiance },
            FriendDiscardThen(ComeToAid, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                attackCardsOnly: false, YouMayMove(ComeToAid, 3, ignoresObstacles: false)),
            ComeToAidText);

        /// <summary>Green, Tier II: Initiative 3, Defense 4, Movement 2, Skill - Ranged, Range 4; Attack item.</summary>
        public static CardDefinition BringTheReliefDefinition { get; } = GreenSkill(
            BringTheRelief, "Bring the Relief", CardTier.II, 3, defense: 4, range: 4, PrintedValueChannel.Attack,
            new[] { CommitReserves, LoyalRetainer },
            FriendDiscardThen(BringTheRelief, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                attackCardsOnly: false, YouMayMove(BringTheRelief, 4, ignoresObstacles: false)),
            BringTheReliefText);

        /// <summary>Green, Tier II: Initiative 3, Defense 4, Movement 2, Skill - Ranged, Range 4; Defense item.</summary>
        public static CardDefinition PledgeOfAllegianceDefinition { get; } = GreenSkill(
            PledgeOfAllegiance, "Pledge of Allegiance", CardTier.II, 3, defense: 4, range: 4,
            PrintedValueChannel.Defense, new[] { CommitReserves, LoyalRetainer },
            FriendDiscardThen(PledgeOfAllegiance, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                attackCardsOnly: false, CoinsAndRetrieve(PledgeOfAllegiance, 1)),
            PledgeOfAllegianceText);

        /// <summary>Green, Tier III: Initiative 3, Defense 4, Movement 2, Skill - Ranged, Range 4; Radius item.</summary>
        public static CardDefinition CommitReservesDefinition { get; } = GreenSkill(
            CommitReserves, "Commit Reserves", CardTier.III, 3, defense: 4, range: 4, PrintedValueChannel.Radius,
            Array.Empty<CardDefinitionId>(),
            FriendDiscardThen(CommitReserves, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                attackCardsOnly: false, YouMayMove(CommitReserves, 4, ignoresObstacles: true)),
            CommitReservesText);

        /// <summary>Green, Tier III: Initiative 3, Defense 4, Movement 2, Skill - Ranged, Range 4; Defense item.</summary>
        public static CardDefinition LoyalRetainerDefinition { get; } = GreenSkill(
            LoyalRetainer, "Loyal Retainer", CardTier.III, 3, defense: 4, range: 4, PrintedValueChannel.Defense,
            Array.Empty<CardDefinitionId>(),
            FriendDiscardThen(LoyalRetainer, SyntheticTargetingCapabilities.RangeHeroTargetV1,
                attackCardsOnly: false, CoinsAndRetrieve(LoyalRetainer, 2)),
            LoyalRetainerText);

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyBlueAndGreen =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                ProvenWarriorDefinition,
                ChosenChampionDefinition,
                CalculatedRiskDefinition,
                TheRightHandDefinition,
                TacticalGambitDefinition,
                ComeToAidDefinition,
                BringTheReliefDefinition,
                PledgeOfAllegianceDefinition,
                CommitReservesDefinition,
                LoyalRetainerDefinition
            }));

        private static CardDefinition BlueSkill(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int radius,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, EffectDefinition text,
            string transcribed) =>
            Skill(card, title, CardColor.Blue, tier, initiative, defense,
                new PrintedValueDefinition(PrintedValueChannel.Radius, radius), item, next, text, transcribed);

        private static CardDefinition GreenSkill(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense, int range,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next, EffectDefinition text,
            string transcribed) =>
            Skill(card, title, CardColor.Green, tier, initiative, defense,
                new PrintedValueDefinition(PrintedValueChannel.Range, range), item, next, text, transcribed);

        private static CardDefinition Skill(
            CardDefinitionId card, string title, CardColor color, CardTier tier, int initiative, int defense,
            PrintedValueDefinition reach, PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next,
            EffectDefinition text, string transcribed) =>
            new CardDefinition(
                card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(color, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { reach },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        text),
                    Movement(2),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(transcribed));

        // -------------------------------------------------------------------------------------------
        // The Blues' same-colour discard
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose a card in the discard of a friendly hero in radius. An enemy hero [Up to two enemy
        /// heroes] in radius discard[s] a card of the same color, if able."
        /// </summary>
        private static EffectDefinition SameColorDiscard(CardDefinitionId card, int enemies)
        {
            var root = Node(card, "skill");
            var friend = Node(card, "friendly-hero-in-radius");
            var chosen = Node(card, "card-in-their-discard");
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: enemies == 1
                        ? new[] { friend, chosen, Node(card, "enemy-0"), Node(card, "discard-0") }
                        : new[] { friend, chosen, Node(card, "pass-0") }),
                FriendlyHero(friend, SyntheticTargetingCapabilities.RadiusHeroTargetV1, consumer: chosen),
                new EffectNodeDefinition(
                    chosen, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDiscardedCardChoiceCapabilities.ChooseDiscardedCardV1,
                    inputs: new[]
                    {
                        ChosenHero(SyntheticDiscardedCardChoiceCapabilities.OwnerParameterName, friend)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticDiscardedCardChoiceCapabilities.ChosenOutputName, EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticDiscardedCardChoiceCapabilities.ColorOutputName, EffectValueKind.Integer)
                    })
            };

            if (enemies == 1)
            {
                nodes.Add(EnemyHeroInRadius(Node(card, "enemy-0"), excluded: null));
                nodes.Add(SameColorDiscardOf(Node(card, "discard-0"), Node(card, "enemy-0"), chosen));
                return Skill(card, root, nodes);
            }

            // W3: each pass is performed when an enemy hero stands there and skipped silently when none
            // does; the second names a different hero.
            for (var pass = 0; pass < enemies; pass++)
            {
                var enemy = Node(card, "enemy-" + pass);
                var discard = Node(card, "discard-" + pass);
                var blast = Node(card, "blast-" + pass);
                nodes.Add(new EffectNodeDefinition(
                    Node(card, "pass-" + pass), EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                    childNodeIds: new[] { blast }));
                nodes.Add(new EffectNodeDefinition(
                    blast, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: pass + 1 < enemies
                        ? new[] { enemy, discard, Node(card, "pass-" + (pass + 1)) }
                        : new[] { enemy, discard }));
                nodes.Add(EnemyHeroInRadius(enemy, pass == 0 ? (EffectNodeId?)null : Node(card, "enemy-" + (pass - 1))));
                nodes.Add(SameColorDiscardOf(discard, enemy, chosen));
            }

            return Skill(card, root, nodes);
        }

        /// <summary>"An enemy hero in radius", picked by Takahide on the Skill's printed Radius with items.</summary>
        private static EffectNodeDefinition EnemyHeroInRadius(EffectNodeId node, EffectNodeId? excluded)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false))
            };
            if (excluded.HasValue)
            {
                inputs.Add(ChosenHero(SyntheticTargetingCapabilities.ExcludedHeroParameterName, excluded.Value));
            }

            return new EffectNodeDefinition(
                node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                });
        }

        /// <summary>"... discards a card of the same color, if able" - the discarding hero chooses which.</summary>
        private static EffectNodeDefinition SameColorDiscardOf(
            EffectNodeId node, EffectNodeId enemy, EffectNodeId chosen) =>
            new EffectNodeDefinition(
                node, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    ChosenHero(SyntheticCardStateCapabilities.HeroParameter, enemy),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.ColorParameter, chosen,
                        SyntheticDiscardedCardChoiceCapabilities.ColorOutputName,
                        EffectValueKind.Integer, EffectIntegerDomain.All<CardColor>())
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                });

        // -------------------------------------------------------------------------------------------
        // A teammate's optional discard and the discard-pile check
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "A friendly hero in radius [range] may discard a[n attack] card. If that hero has a card in the
        /// discard, ..." - the friendly hero, their optional discard on their own seat, the pile read
        /// after it, and the printed consequence under the check (a false check does nothing, RULING 53).
        /// </summary>
        private static EffectDefinition FriendDiscardThen(
            CardDefinitionId card, EffectCapabilityReference reach, bool attackCardsOnly,
            IReadOnlyList<EffectNodeDefinition> consequence)
        {
            var root = Node(card, "skill");
            var friend = Node(card, "friendly-hero");
            var discard = Node(card, "friend-may-discard");
            var present = Node(card, "friend-has-a-discard");
            var gate = Node(card, "if-that-hero-has-a-card-in-the-discard");
            var discardInputs = new List<EffectInputBinding>
            {
                ChosenHero(SyntheticCardStateCapabilities.HeroParameter, friend)
            };
            if (attackCardsOnly)
            {
                discardInputs.Add(EffectInputBinding.FromLiteral(
                    SyntheticCardStateCapabilities.AttackCardsOnlyParameter, EffectValue.Boolean(true)));
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { friend, discard, present, gate }),
                FriendlyHero(friend, reach, consumer: null),
                // "may discard": their decision, asked of their seat by the discard's own chooser.
                new EffectNodeDefinition(
                    discard, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: discardInputs,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                    }),
                // "has a card in the discard": the state of the pile, not "if they do".
                new EffectNodeDefinition(
                    present, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.OwnedCardPresentV1,
                    inputs: new[]
                    {
                        ChosenHero(SyntheticRosterCapabilities.HeroParameter, friend),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.LocationParameter, CardLocation.DiscardPile)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { consequence[0].Id },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "flag", present, SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    },
                    outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) })
            };
            nodes.AddRange(consequence);
            return Skill(card, root, nodes);
        }

        /// <summary>"that hero may move up to 2 spaces[, ignoring obstacles]" - their card-text move (RO-Q2).</summary>
        private static IReadOnlyList<EffectNodeDefinition> FriendMayMove(CardDefinitionId card, bool ignoresObstacles)
        {
            var friend = Node(card, "friendly-hero");
            return new[]
            {
                Move(Node(card, "that-hero-may-move"),
                    EffectInputBinding.FromNodeOutput(
                        "mover", friend, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                        EffectValueKind.Coordinate),
                    2, ignoresObstacles,
                    ChosenHero(SyntheticDisplacementCapabilities.ChooserParameterName, friend))
            };
        }

        /// <summary>"you may move up to 3 [4] spaces[, ignoring obstacles]" - Takahide's card-text move.</summary>
        private static IReadOnlyList<EffectNodeDefinition> YouMayMove(
            CardDefinitionId card, int distance, bool ignoresObstacles)
        {
            var sequence = Node(card, "you-may-move");
            var space = Node(card, "your-space");
            return new[]
            {
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { space, Node(card, "move") }),
                new EffectNodeDefinition(
                    space, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                Move(Node(card, "move"),
                    EffectInputBinding.FromNodeOutput("mover", space, "space", EffectValueKind.Coordinate),
                    distance, ignoresObstacles, chooser: null)
            };
        }

        /// <summary>
        /// p.11's "up to" on a card-text move (not a Movement action, so no Fast Travel and no Movement
        /// buffs); "ignoring obstacles" crosses them and still never ends on one (p.18).
        /// </summary>
        private static EffectNodeDefinition Move(
            EffectNodeId node, EffectInputBinding mover, int distance, bool ignoresObstacles,
            EffectInputBinding? chooser)
        {
            var inputs = new List<EffectInputBinding>
            {
                mover,
                EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                    DirectMoveDestinationPolicy.Unrestricted)
            };
            if (ignoresObstacles)
            {
                inputs.Add(EffectInputBinding.FromLiteral(
                    SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName, EffectValue.Boolean(true)));
            }

            if (chooser.HasValue)
            {
                inputs.Add(chooser.Value);
            }

            return new EffectNodeDefinition(
                node, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: inputs,
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) });
        }

        /// <summary>
        /// "both you and that hero gain 1 coin [2 coins] and you may retrieve a discarded card" - two
        /// grants and his own optional retrieval (p.10: you never retrieve another hero's cards).
        /// </summary>
        private static IReadOnlyList<EffectNodeDefinition> CoinsAndRetrieve(CardDefinitionId card, int coins)
        {
            var friend = Node(card, "friendly-hero");
            var sequence = Node(card, "coins-and-retrieve");
            var yours = Node(card, "you-gain");
            var theirs = Node(card, "that-hero-gains");
            var retrieve = Node(card, "you-may-retrieve");
            EffectOutputBinding[] grantOutputs() => new[]
            {
                new EffectOutputBinding("recipient", EffectValueKind.Hero),
                new EffectOutputBinding("amount", EffectValueKind.Integer)
            };
            return new[]
            {
                new EffectNodeDefinition(
                    sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { yours, theirs, retrieve }),
                new EffectNodeDefinition(
                    yours, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                    },
                    outputs: grantOutputs()),
                new EffectNodeDefinition(
                    theirs, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        ChosenHero("recipient", friend),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(coins))
                    },
                    outputs: grantOutputs()),
                new EffectNodeDefinition(
                    retrieve, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticRosterCapabilities.RetrieveCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("owner", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromRole("chooser", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary("kind", RosterCardRetrievalKind.OwnedDiscardedCard)
                    },
                    outputs: new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) })
            };
        }

        // -------------------------------------------------------------------------------------------
        // Shared
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "A friendly hero in radius [range]": allied, never himself (p.10 Picking Targets), on the
        /// Skill's printed icon with items. Mandatory, so with nobody to pick the face is greyed (TK-Q4).
        /// </summary>
        private static EffectNodeDefinition FriendlyHero(
            EffectNodeId node, EffectCapabilityReference reach, EffectNodeId? consumer)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.AlliedWithSource)
            };
            inputs.AddRange(reach.Equals(SyntheticTargetingCapabilities.RadiusHeroTargetV1)
                ? new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false))
                }
                : new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName, EffectValue.Boolean(false))
                });
            if (consumer.HasValue)
            {
                inputs.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer.Value)));
            }

            return new EffectNodeDefinition(
                node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, reach,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName, EffectValueKind.Coordinate)
                });
        }

        private static EffectInputBinding ChosenHero(string parameter, EffectNodeId selection) =>
            EffectInputBinding.FromNodeOutput(
                parameter, selection, SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero);

        private static EffectDefinition Skill(
            CardDefinitionId card, EffectNodeId root, IEnumerable<EffectNodeDefinition> nodes) =>
            new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), ContentRevision, root, nodes);

        private static EffectNodeId Node(CardDefinitionId card, string name) =>
            new EffectNodeId(card.Value + "-" + name);
    }
}

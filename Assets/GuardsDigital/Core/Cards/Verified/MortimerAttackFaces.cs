using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8c. Mortimer's Gold basic and his five Red Attacks. Every one prints "Target a unit
    /// adjacent to you" - p.13's own non-ranged reading, so the target is chosen by the Attack and the
    /// text runs with it bound - and every one prints the Radius 4 icon its text reads.
    ///
    /// <para><b>Knife of the Living Dead</b> (Gold): "Before the attack, Choose up to three times -
    /// Move a Zombie token in radius 1 space. / Remove a Zombie token adjacent to the target; if you
    /// do, gain +1 Attack." Each Remove that happens adds one to this Attack
    /// (<see cref="MortimerComponentContent.GainAttackForPass"/>).</para>
    ///
    /// <para><b>Crawling, Walking and Racing Dead</b>: "After the attack, choose one / up to two /
    /// up to three times - Move a Zombie token in radius 1 space. / Another enemy hero in radius,
    /// adjacent to a Zombie token, discards a card, if able" - Walking and Racing adding "; each enemy
    /// hero can only be targeted once", which is the selection group
    /// (<see cref="EffectSelectionDeclaration.DistinctAcrossParameterName"/>) every pass's hero
    /// selection joins. "Another" is <see cref="SyntheticTargetingCapabilities.ExcludeTheAttackTargetParameterName"/>.
    /// "If able" is the discard's own policy, as Garrus's Light Pilum and Sabina's Close Support
    /// compose it: the hero is chosen whatever their hand holds.</para>
    ///
    /// <para><b>Braains...! and Braaaaaaaains...!</b> (the Tier III card prints eight a's, mortimer7__7703468;
    /// 9.14 corrected the display name from seven, the identity id staying <c>braaaaaaains</c>): "Before the attack, choose up to two / three
    /// times - Move a Zombie token in radius 1 space. / If an enemy hero in radius is adjacent to a
    /// Zombie token, retrieve a discarded card." The "If" is printed, so it is a single-child
    /// Conditional (RULING 53 for the whole action; as a modal option the gate is read and a false
    /// one leaves the bullet unavailable).</para>
    ///
    /// <para>Nothing here is registered until 9.8f.</para>
    /// </summary>
    public static class MortimerAttackFaces
    {
        public static CardDefinitionId KnifeOfTheLivingDead { get; } = MortimerCardContent.Id("knife-of-the-living-dead");
        public static CardDefinitionId CrawlingDead { get; } = MortimerCardContent.Id("crawling-dead");
        public static CardDefinitionId WalkingDead { get; } = MortimerCardContent.Id("walking-dead");
        public static CardDefinitionId Braains { get; } = MortimerCardContent.Id("braains");
        public static CardDefinitionId RacingDead { get; } = MortimerCardContent.Id("racing-dead");
        public static CardDefinitionId Braaaaaaains { get; } = MortimerCardContent.Id("braaaaaaains");

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public const string KnifeText =
            "Target a unit adjacent to you. Before the attack, Choose up to three times - Move a Zombie " +
            "token in radius 1 space. / Remove a Zombie token adjacent to the target; if you do, gain " +
            "+1 Attack.";

        public const string CrawlingText =
            "Target a unit adjacent to you. After the attack, choose one - Move a Zombie token in radius " +
            "1 space. / Another enemy hero in radius, adjacent to a Zombie token, discards a card, if able.";

        public static string DeadText(int passes) =>
            "Target a unit adjacent to you. After the attack, choose up to " + Times(passes) + " - Move a " +
            "Zombie token in radius 1 space. / Another enemy hero in radius, adjacent to a Zombie token, " +
            "discards a card, if able; each enemy hero can only be targeted once.";

        public static string BrainsText(int passes) =>
            "Target a unit adjacent to you. Before the attack, choose up to " + Times(passes) + " - Move " +
            "a Zombie token in radius 1 space. / If an enemy hero in radius is adjacent to a Zombie " +
            "token, retrieve a discarded card.";

        private static string Times(int passes) => passes == 2 ? "two times" : "three times";

        /// <summary>The pass prefixes, so a test or a journey names the node it expects.</summary>
        public const string KnifePrefix = "knife";
        public const string DeadPrefix = "dead";
        public const string BrainsPrefix = "brains";

        public static string MoveBullet(string prefix, int pass) => $"{prefix}-{pass}-move";
        public static string RemoveBullet(int pass) => $"{KnifePrefix}-{pass}-remove";
        public static string DiscardBullet(int pass) => $"{DeadPrefix}-{pass}-discard";
        public static string RetrieveBullet(int pass) => $"{BrainsPrefix}-{pass}-retrieve";

        /// <summary>Knife and the five Reds, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => Lazy.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> Lazy =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Face(KnifeOfTheLivingDead, "Knife of the Living Dead", 11, 3, 1, 4,
                    CardProgressionProfile.BasicCard(CardColor.Gold),
                    Body(KnifeOfTheLivingDead, KnifePrefix, 3, KnifePass), CardTextTiming.BeforeTheAttack,
                    KnifeText),
                Face(CrawlingDead, "Crawling Dead", 7, 7, 3, 6,
                    Red(CardTier.I, PrintedValueChannel.None, WalkingDead, Braains),
                    Body(CrawlingDead, DeadPrefix, 1, pass => DeadPass(pass, false)), CardTextTiming.AfterTheAttack,
                    CrawlingText),
                Face(WalkingDead, "Walking Dead", 7, 7, 3, 7,
                    Red(CardTier.II, PrintedValueChannel.Defense, RacingDead, Braaaaaaains),
                    Body(WalkingDead, DeadPrefix, 2, pass => DeadPass(pass, true)), CardTextTiming.AfterTheAttack,
                    DeadText(2)),
                Face(Braains, "Braains...!", 7, 7, 3, 7,
                    Red(CardTier.II, PrintedValueChannel.Initiative, RacingDead, Braaaaaaains),
                    Body(Braains, BrainsPrefix, 2, BrainsPass), CardTextTiming.BeforeTheAttack,
                    BrainsText(2)),
                Face(RacingDead, "Racing Dead", 8, 8, 3, 7,
                    Red(CardTier.III, PrintedValueChannel.Range),
                    Body(RacingDead, DeadPrefix, 3, pass => DeadPass(pass, true)), CardTextTiming.AfterTheAttack,
                    DeadText(3)),
                Face(Braaaaaaains, "Braaaaaaaains...!", 8, 8, 3, 7,
                    Red(CardTier.III, PrintedValueChannel.Movement),
                    Body(Braaaaaaains, BrainsPrefix, 3, BrainsPass), CardTextTiming.BeforeTheAttack,
                    BrainsText(3))
            }));

        private static CardProgressionProfile Red(
            CardTier tier, PrintedValueChannel item, params CardDefinitionId[] next) =>
            CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next);

        private static CardDefinition Face(
            CardDefinitionId card, string title, int initiative, int defense, int movement, int attack,
            CardProgressionProfile progression, EffectDefinition text, CardTextTiming timing,
            string printed) =>
            new CardDefinition(
                card, MortimerCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                progression,
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 4)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        text,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        timing,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                    MortimerCardContent.Movement(movement),
                    MortimerCardContent.Defense(defense)
                },
                null, CardSourceContent.Transcribed(printed));

        private static EffectDefinition Body(
            CardDefinitionId card, string prefix, int passes,
            Func<int, (IReadOnlyList<EffectNodeId> Options, IEnumerable<EffectNodeDefinition> Nodes)> pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var steps = MortimerRepeatedChoice.Compose(prefix, passes, pass, nodes);
            var root = new EffectNodeId($"{prefix}-root");
            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, MortimerRepeatedChoice.CountPolicy(passes), childNodeIds: steps));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), MortimerCardContent.ContentRevision,
                root, nodes);
        }

        // --- The passes -----------------------------------------------------------------------------

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) KnifePass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var move = MoveZombie(KnifePrefix, pass, nodes);
            var remove = RemoveZombieBesideTheTarget(pass, nodes);
            return (new[] { move, remove }, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) DeadPass(
            int pass, bool eachHeroOnce)
        {
            var nodes = new List<EffectNodeDefinition>();
            var move = MoveZombie(DeadPrefix, pass, nodes);
            var discard = AnotherEnemyHeroDiscards(pass, eachHeroOnce, nodes);
            return (new[] { move, discard }, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) BrainsPass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var move = MoveZombie(BrainsPrefix, pass, nodes);
            var retrieve = RetrieveIfAnEnemyHeroStandsBesideAZombie(pass, nodes);
            return (new[] { move, retrieve }, nodes);
        }

        // --- The bullets ----------------------------------------------------------------------------

        /// <summary>
        /// "Move a Zombie token in radius 1 space": a Zombie standing in the printed Radius of Mortimer,
        /// moved exactly one space (p.11: an unqualified number is exact) to an empty space.
        /// </summary>
        private static EffectNodeId MoveZombie(string prefix, int pass, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(MoveBullet(prefix, pass));
            var select = new EffectNodeId($"{prefix}-{pass}-move-zombie");
            var move = new EffectNodeId($"{prefix}-{pass}-move-step");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { select, move }));
            nodes.Add(ZombieInRadius(select, move));
            nodes.Add(new EffectNodeDefinition(
                move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("mover", select, "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }));
            return bullet;
        }

        /// <summary>
        /// "Remove a Zombie token adjacent to the target; if you do, gain +1 Attack": the attacked
        /// space, a Zombie beside it, p.19's Remove, and - only because the Remove happened, which a
        /// mandatory Sequence says - this pass's +1.
        /// </summary>
        private static EffectNodeId RemoveZombieBesideTheTarget(int pass, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(RemoveBullet(pass));
            var targetSpace = new EffectNodeId($"{KnifePrefix}-{pass}-target-space");
            var select = new EffectNodeId($"{KnifePrefix}-{pass}-remove-zombie");
            var remove = new EffectNodeId($"{KnifePrefix}-{pass}-remove-step");
            var gain = new EffectNodeId($"{KnifePrefix}-{pass}-gain-attack");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { targetSpace, select, remove, gain }));
            nodes.Add(new EffectNodeDefinition(
                targetSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.FormerTargetSpaceV1,
                outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }));
            nodes.Add(ZombieInRadius(select, remove,
                EffectInputBinding.FromNodeOutput(
                    SyntheticTargetingCapabilities.AdjacentToSpaceParameterName, targetSpace, "space",
                    EffectValueKind.Coordinate)));
            nodes.Add(new EffectNodeDefinition(
                remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.RemoveV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("subject", select, "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName, RemoveEligiblePieceKind.Token)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                    new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                }));
            nodes.Add(new EffectNodeDefinition(
                gain, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                MortimerComponentContent.GainAttackForPass(pass),
                outputs: new[]
                {
                    new EffectOutputBinding(ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                }));
            return bullet;
        }

        /// <summary>
        /// "Another enemy hero in radius, adjacent to a Zombie token, discards a card, if able" - and on
        /// the repeated faces, "each enemy hero can only be targeted once": every pass's selection joins
        /// one group anchored on the first pass's.
        /// </summary>
        private static EffectNodeId AnotherEnemyHeroDiscards(
            int pass, bool eachHeroOnce, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(DiscardBullet(pass));
            var select = new EffectNodeId($"{DeadPrefix}-{pass}-discard-hero");
            var discard = new EffectNodeId($"{DeadPrefix}-{pass}-discard-step");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { select, discard }));
            var clauses = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                    EffectValue.TokenKind(MortimerComponentContent.ZombieId)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeTheAttackTargetParameterName, EffectValue.Boolean(true)),
                EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(discard))
            };
            if (eachHeroOnce)
            {
                clauses.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.DistinctAcrossParameterName,
                    EffectValue.Node(new EffectNodeId($"{DeadPrefix}-1-discard-hero"))));
            }

            nodes.Add(new EffectNodeDefinition(
                select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: clauses,
                outputs: new[] { new EffectOutputBinding("chosenHero", EffectValueKind.Hero) }));
            nodes.Add(new EffectNodeDefinition(
                discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[] { EffectInputBinding.FromNodeOutput("hero", select, "chosenHero", EffectValueKind.Hero) },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                }));
            return bullet;
        }

        /// <summary>
        /// "If an enemy hero in radius is adjacent to a Zombie token, retrieve a discarded card": the
        /// card's printed Radius, the printed "If" over presence-count's token-adjacency clause, and
        /// p.17's Retrieve of one of his own discarded cards.
        /// </summary>
        private static EffectNodeId RetrieveIfAnEnemyHeroStandsBesideAZombie(int pass, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(RetrieveBullet(pass));
            var radius = new EffectNodeId($"{BrainsPrefix}-{pass}-radius");
            var gate = new EffectNodeId($"{BrainsPrefix}-{pass}-if-beside-zombie");
            var retrieve = new EffectNodeId($"{BrainsPrefix}-{pass}-retrieve-step");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { radius, gate }));
            nodes.Add(new EffectNodeDefinition(
                radius, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticValueCapabilities.CardRadiusV1,
                outputs: new[] { new EffectOutputBinding("radius", EffectValueKind.Integer) }));
            nodes.Add(new EffectNodeDefinition(
                gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.PresenceCountV1,
                childNodeIds: new[] { retrieve },
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticRosterCapabilities.DistanceParameter, radius, "radius", EffectValueKind.Integer),
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral("excludeActor", EffectValue.Boolean(true)),
                    EffectInputBinding.FromLiteral("excludeTarget", EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                        EffectValue.TokenKind(MortimerComponentContent.ZombieId))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("count", EffectValueKind.Integer),
                    new EffectOutputBinding("present", EffectValueKind.Boolean)
                }));
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
            return bullet;
        }

        /// <summary>A Zombie token standing in the card's printed Radius of Mortimer.</summary>
        private static EffectNodeDefinition ZombieInRadius(
            EffectNodeId select, EffectNodeId consumer, params EffectInputBinding[] clauses)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.AdmittedObjectKindParameterName, PrintedIconObjectKind.TokensOnly),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.TokenKindParameterName,
                    EffectValue.TokenKind(MortimerComponentContent.ZombieId)),
                EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer))
            };
            inputs.AddRange(clauses);
            return new EffectNodeDefinition(
                select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusTargetV1,
                inputs: inputs,
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
        }
    }
}

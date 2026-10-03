using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8e. Mortimer's five Green Skills - Ranged Skills with the Range 4 icon, composed on
    /// <see cref="MortimerRepeatedChoice"/>.
    ///
    /// <para><b>Stage Dive, Crowd Drift, Crowd Surf</b>: "Choose one / up to two / up to three times -
    /// Move a Zombie token in range 1 space. / Swap with a Zombie token in range." The move is exact
    /// (p.11); the swap is p.19's Swap between Mortimer and the Zombie.</para>
    ///
    /// <para><b>Gathering Horde, Army of Darkness</b>: "Choose up to two / three times - Move a Zombie
    /// token in range 1 space. / Once per turn: Replace an enemy minion in range adjacent to two or
    /// more Zombie tokens with a Zombie token." The minion selection is the printed-icon minion arm
    /// with <c>adjacentToTokenKind</c> and <c>adjacentAtLeast</c> 2; "once per turn" is every pass's
    /// selection in one group with <see cref="SyntheticTargetingCapabilities.OnceAcrossTheGroupParameterName"/>;
    /// the Replace takes the minion off the board (removed, not defeated: no coins; a p.8 Lane-Push
    /// cause) and puts a Zombie in its space, after p.18's short-supply step. MO-Q4: that Zombie stays
    /// at the end of the round like every other.</para>
    ///
    /// <para>Nothing here is registered until 9.8f.</para>
    /// </summary>
    public static class MortimerGreenFaces
    {
        public static CardDefinitionId StageDive { get; } = MortimerCardContent.Id("stage-dive");
        public static CardDefinitionId CrowdDrift { get; } = MortimerCardContent.Id("crowd-drift");
        public static CardDefinitionId GatheringHorde { get; } = MortimerCardContent.Id("gathering-horde");
        public static CardDefinitionId CrowdSurf { get; } = MortimerCardContent.Id("crowd-surf");
        public static CardDefinitionId ArmyOfDarkness { get; } = MortimerCardContent.Id("army-of-darkness");

        public const string DivePrefix = "dive";
        public const string HordePrefix = "horde";

        public static string MoveBullet(string prefix, int pass) => $"{prefix}-{pass}-move";
        public static string SwapBullet(int pass) => $"{DivePrefix}-{pass}-swap";
        public static string ReplaceBullet(int pass) => $"{HordePrefix}-{pass}-replace";

        public const string StageDiveText =
            "Choose one - Move a Zombie token in range 1 space. / Swap with a Zombie token in range.";

        public static string CrowdText(int passes) =>
            "Choose up to " + Times(passes) + " - Move a Zombie token in range 1 space. / Swap with a " +
            "Zombie token in range.";

        public static string HordeText(int passes) =>
            "Choose up to " + Times(passes) + " - Move a Zombie token in range 1 space. / Once per " +
            "turn: Replace an enemy minion in range adjacent to two or more Zombie tokens with a Zombie token.";

        private static string Times(int passes) => passes == 2 ? "two times" : "three times";

        /// <summary>The five Greens, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => Lazy.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> Lazy =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Face(StageDive, "Stage Dive", CardTier.I, 5, 3, PrintedValueChannel.None,
                    new[] { CrowdDrift, GatheringHorde }, DivePrefix, 1, DivePass, StageDiveText),
                Face(CrowdDrift, "Crowd Drift", CardTier.II, 5, 4, PrintedValueChannel.Attack,
                    new[] { CrowdSurf, ArmyOfDarkness }, DivePrefix, 2, DivePass, CrowdText(2)),
                Face(GatheringHorde, "Gathering Horde", CardTier.II, 5, 4, PrintedValueChannel.Initiative,
                    new[] { CrowdSurf, ArmyOfDarkness }, HordePrefix, 2, HordePass, HordeText(2)),
                Face(CrowdSurf, "Crowd Surf", CardTier.III, 4, 4, PrintedValueChannel.Attack,
                    Array.Empty<CardDefinitionId>(), DivePrefix, 3, DivePass, CrowdText(3)),
                Face(ArmyOfDarkness, "Army of Darkness", CardTier.III, 4, 4, PrintedValueChannel.Initiative,
                    Array.Empty<CardDefinitionId>(), HordePrefix, 3, HordePass, HordeText(3))
            }));

        private static CardDefinition Face(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            PrintedValueChannel item, CardDefinitionId[] next, string prefix, int passes,
            Func<int, (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>)> pass,
            string printed) =>
            new CardDefinition(
                card, MortimerCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MortimerCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        Body(card, prefix, passes, pass)),
                    MortimerCardContent.Movement(2),
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
                new EffectDefinitionId(card.Value + "-skill"), MortimerCardContent.ContentRevision, root, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) DivePass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var move = MoveZombie(DivePrefix, pass, nodes);

            // "Swap with a Zombie token in range."
            var swap = new EffectNodeId(SwapBullet(pass));
            var select = new EffectNodeId($"{DivePrefix}-{pass}-swap-zombie");
            var exchange = new EffectNodeId($"{DivePrefix}-{pass}-swap-step");
            nodes.Add(new EffectNodeDefinition(
                swap, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { select, exchange }));
            nodes.Add(ZombieInRange(select, exchange));
            nodes.Add(new EffectNodeDefinition(
                exchange, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapWithActorV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("subject", select, "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                }));
            return (new[] { move, swap }, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) HordePass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var move = MoveZombie(HordePrefix, pass, nodes);

            // "Once per turn: Replace an enemy minion in range adjacent to two or more Zombie tokens
            // with a Zombie token."
            var replace = new EffectNodeId(ReplaceBullet(pass));
            var select = new EffectNodeId($"{HordePrefix}-{pass}-replace-minion");
            var reclaim = new EffectNodeId($"{HordePrefix}-{pass}-replace-reclaim");
            var substitute = new EffectNodeId($"{HordePrefix}-{pass}-replace-step");
            nodes.Add(new EffectNodeDefinition(
                replace, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { select, reclaim, substitute }));
            nodes.Add(new EffectNodeDefinition(
                select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RangeMinionTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName,
                        EffectValue.TokenKind(MortimerComponentContent.ZombieId)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.AdjacentAtLeastParameterName, EffectValue.Integer(2)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.DistinctAcrossParameterName,
                        EffectValue.Node(new EffectNodeId($"{HordePrefix}-1-replace-minion"))),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.OnceAcrossTheGroupParameterName, EffectValue.Boolean(true)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(substitute))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenMinionOutputName, EffectValueKind.Minion),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                }));
            nodes.Add(new EffectNodeDefinition(
                reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                MortimerComponentContent.ReclaimZombieV1,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                }));
            nodes.Add(new EffectNodeDefinition(
                substitute, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                MortimerComponentContent.ReplaceMinionWithZombieV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        "subject", select, SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                        EffectValueKind.Coordinate)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTokenCapabilities.ReplacedOutput, EffectValueKind.Coordinate),
                    new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                }));
            return (new[] { move, replace }, nodes);
        }

        /// <summary>"Move a Zombie token in range 1 space": exactly one space (p.11).</summary>
        private static EffectNodeId MoveZombie(string prefix, int pass, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(MoveBullet(prefix, pass));
            var select = new EffectNodeId($"{prefix}-{pass}-move-zombie");
            var move = new EffectNodeId($"{prefix}-{pass}-move-step");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { select, move }));
            nodes.Add(ZombieInRange(select, move));
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

        /// <summary>A Zombie token standing in the card's printed Range of Mortimer.</summary>
        private static EffectNodeDefinition ZombieInRange(EffectNodeId select, EffectNodeId consumer) =>
            new EffectNodeDefinition(
                select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RangeTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeStraightLineParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.AdmittedObjectKindParameterName, PrintedIconObjectKind.TokensOnly),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.TokenKindParameterName,
                        EffectValue.TokenKind(MortimerComponentContent.ZombieId)),
                    EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer))
                },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
    }
}

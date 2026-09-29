using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.8d. Mortimer's five Blue Skills - each a Ranged Skill with the Range 4 icon its
    /// text reads, composed on <see cref="MortimerRepeatedChoice"/>.
    ///
    /// <para><b>Corpse Slam, Morbid Mosh, Macabre Mayhem</b>: "Choose one / up to two / up to three
    /// times - Move a Zombie token in range up to 1 space; that Zombie token may push a unit or a token
    /// adjacent to it 1 space. / Move 1 space." The Zombie moves up to one space (W5: 0 is doing it),
    /// and "that Zombie token" is the push's pusher: p.18's Push made FROM the Zombie's space
    /// (<see cref="SyntheticDisplacementCapabilities.PusherSpaceParameterName"/>), directly away from
    /// it, taking any one unit (friend, foe or Mortimer himself) or token beside it.</para>
    ///
    /// <para><b>Robbing Zombies, Stalking Scalpers</b>: "Choose up to two / three times - Move a
    /// Zombie token in range up to 1 space. Gain 1 coin. / Move 1 space." MO-Q3: the coin follows the
    /// move in one mandatory Sequence, so with no Zombie in range the bullet cannot start (p.10) and no
    /// coin is gained; a Zombie that cannot or does not move still moved "up to 1".</para>
    ///
    /// <para>"Move 1 space" is Mortimer's own exact one-space move (p.11). Nothing here is registered
    /// until 9.8f.</para>
    /// </summary>
    public static class MortimerBlueFaces
    {
        public static CardDefinitionId CorpseSlam { get; } = MortimerCardContent.Id("corpse-slam");
        public static CardDefinitionId MorbidMosh { get; } = MortimerCardContent.Id("morbid-mosh");
        public static CardDefinitionId RobbingZombies { get; } = MortimerCardContent.Id("robbing-zombies");
        public static CardDefinitionId MacabreMayhem { get; } = MortimerCardContent.Id("macabre-mayhem");
        public static CardDefinitionId StalkingScalpers { get; } = MortimerCardContent.Id("stalking-scalpers");

        public const string SlamPrefix = "slam";
        public const string RobPrefix = "rob";

        public static string ShoveBullet(int pass) => $"{SlamPrefix}-{pass}-zombie";
        public static string CoinBullet(int pass) => $"{RobPrefix}-{pass}-zombie";
        public static string StepBullet(string prefix, int pass) => $"{prefix}-{pass}-step";

        public const string CorpseSlamText =
            "Choose one - Move a Zombie token in range up to 1 space; that Zombie token may push a unit " +
            "or a token adjacent to it 1 space. / Move 1 space.";

        public static string MoshText(int passes) =>
            "Choose up to " + Times(passes) + " - Move a Zombie token in range up to 1 space; that " +
            "Zombie token may push a unit or a token adjacent to it 1 space. / Move 1 space.";

        public static string RobbingText(int passes) =>
            "Choose up to " + Times(passes) + " - Move a Zombie token in range up to 1 space. Gain 1 " +
            "coin. / Move 1 space.";

        private static string Times(int passes) => passes == 2 ? "two times" : "three times";

        /// <summary>The five Blues, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => Lazy.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> Lazy =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Face(CorpseSlam, "Corpse Slam", CardTier.I, 9, 6, PrintedValueChannel.None,
                    new[] { MorbidMosh, RobbingZombies }, SlamPrefix, 1, ShovePass, CorpseSlamText),
                Face(MorbidMosh, "Morbid Mosh", CardTier.II, 9, 6, PrintedValueChannel.Attack,
                    new[] { MacabreMayhem, StalkingScalpers }, SlamPrefix, 2, ShovePass, MoshText(2)),
                Face(RobbingZombies, "Robbing Zombies", CardTier.II, 9, 6, PrintedValueChannel.Defense,
                    new[] { MacabreMayhem, StalkingScalpers }, RobPrefix, 2, CoinPass, RobbingText(2)),
                Face(MacabreMayhem, "Macabre Mayhem", CardTier.III, 10, 7, PrintedValueChannel.Radius,
                    Array.Empty<CardDefinitionId>(), SlamPrefix, 3, ShovePass, MoshText(3)),
                Face(StalkingScalpers, "Stalking Scalpers", CardTier.III, 10, 7, PrintedValueChannel.Defense,
                    Array.Empty<CardDefinitionId>(), RobPrefix, 3, CoinPass, RobbingText(3))
            }));

        private static CardDefinition Face(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            PrintedValueChannel item, CardDefinitionId[] next, string prefix, int passes,
            Func<int, (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>)> pass,
            string printed) =>
            new CardDefinition(
                card, MortimerCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        MortimerCardContent.PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        Body(card, prefix, passes, pass)),
                    MortimerCardContent.Movement(3),
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
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: steps));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), MortimerCardContent.ContentRevision, root, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) ShovePass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var zombie = ZombieMovesThen(SlamPrefix, pass, nodes, (move, then) =>
            {
                // "; that Zombie token may push a unit or a token adjacent to it 1 space."
                var push = new EffectNodeId($"{SlamPrefix}-{pass}-push");
                nodes.Add(new EffectNodeDefinition(
                    then, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { push }, chooser: EffectChooserRole.Actor));
                nodes.Add(new EffectNodeDefinition(
                    push, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticDisplacementCapabilities.ObjectPushV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName, EffectValue.Integer(0)),
                        EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName, PushEligiblePieceKind.UnitOrToken),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.TargetRelationParameterName, SourceTeamRelation.AnyTeam),
                        EffectInputBinding.FromLiteral(
                            SyntheticDisplacementCapabilities.MaximumTargetsParameterName, EffectValue.Integer(1)),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticDisplacementCapabilities.PusherSpaceParameterName, move, "moved",
                            EffectValueKind.Coordinate)
                    },
                    outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Integer) }));
            });
            var step = MortimerSteps(SlamPrefix, pass, nodes);
            return (new[] { zombie, step }, nodes);
        }

        private static (IReadOnlyList<EffectNodeId>, IEnumerable<EffectNodeDefinition>) CoinPass(int pass)
        {
            var nodes = new List<EffectNodeDefinition>();
            var zombie = ZombieMovesThen(RobPrefix, pass, nodes, (move, then) =>
            {
                // "Gain 1 coin."
                nodes.Add(new EffectNodeDefinition(
                    then, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.GainResourceV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole("recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral("amount", EffectValue.Integer(1))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    }));
            });
            var step = MortimerSteps(RobPrefix, pass, nodes);
            return (new[] { zombie, step }, nodes);
        }

        /// <summary>
        /// "Move a Zombie token in range up to 1 space" and what the bullet does next: a Zombie in the
        /// printed Range, moved 0 or 1 spaces (the <see cref="EffectFailurePolicy.UpTo"/> distance, so
        /// standing still is an answer and "moved" is always published), then <paramref name="then"/>.
        /// </summary>
        private static EffectNodeId ZombieMovesThen(
            string prefix, int pass, List<EffectNodeDefinition> nodes, Action<EffectNodeId, EffectNodeId> then)
        {
            var bullet = new EffectNodeId($"{prefix}-{pass}-zombie");
            var select = new EffectNodeId($"{prefix}-{pass}-zombie-select");
            var move = new EffectNodeId($"{prefix}-{pass}-zombie-move");
            var after = new EffectNodeId($"{prefix}-{pass}-zombie-then");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: new[] { select, move, after }));
            nodes.Add(new EffectNodeDefinition(
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
                    EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(move))
                },
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }));
            nodes.Add(new EffectNodeDefinition(
                move, EffectNodeKind.Capability, EffectFailurePolicy.UpTo,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("mover", select, "chosen", EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromLiteral(
                        SyntheticDisplacementCapabilities.LeastDistanceParameterName, EffectValue.Integer(0)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }));
            then(move, after);
            return bullet;
        }

        /// <summary>"Move 1 space": Mortimer's own exact one-space move.</summary>
        private static EffectNodeId MortimerSteps(string prefix, int pass, List<EffectNodeDefinition> nodes)
        {
            var bullet = new EffectNodeId(StepBullet(prefix, pass));
            var space = new EffectNodeId($"{prefix}-{pass}-step-space");
            var move = new EffectNodeId($"{prefix}-{pass}-step-move");
            nodes.Add(new EffectNodeDefinition(
                bullet, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { space, move }));
            nodes.Add(new EffectNodeDefinition(
                space, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticPlacementCapabilities.ActorSpaceV1,
                outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }));
            nodes.Add(new EffectNodeDefinition(
                move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("mover", space, "space", EffectValueKind.Coordinate),
                    EffectInputBinding.FromLiteral("distance", EffectValue.Integer(1)),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                        DirectMoveDestinationPolicy.Unrestricted)
                },
                outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) }));
            return bullet;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Mortimer the Awakener's printed content. Milestone 9.8a starts it with his Silver basic.
    ///
    /// <para><b>Awaken!</b> (Silver): "Place up to 4 Zombie tokens into spawn points in radius,
    /// counting spaces adjacent to you as spaces with spawn points; Zombie tokens are not removed at
    /// the end of round." Four optional supply Places reaching the printed Radius 4, each preceded by
    /// p.18's short-supply step - Mystic Saplings' composition - with each Place declaring where a
    /// Zombie may go: any spawn point (minion or hero) or a space adjacent to him (MO-Q2). The second
    /// sentence is the Zombie's own declared lifetime (MO-Q4,
    /// <see cref="MortimerComponentContent.Zombie"/>), so it composes to no node.</para>
    ///
    /// <para>Nothing here is registered until 9.8f deals him.</para>
    /// </summary>
    public static partial class MortimerCardContent
    {
        /// <summary>The revision every Mortimer definition carries.</summary>
        public const string ContentRevision = "mortimer-9-8-rev-1";

        public const string ContentSetId = "mortimer";

        public static CardDefinitionId Awaken { get; } = Id("awaken");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// Milestone 9.8f. Awaken!'s Defense, which is a PRIMARY: the face prints "Basic Defense /
        /// Skill", p.13/14's Defense/Skill card.
        /// </summary>
        public static ActionDefinitionId PrimaryDefense { get; } = new ActionDefinitionId("primary-defense");

        public const string AwakenText =
            "Place up to 4 Zombie tokens into spawn points in radius, counting spaces adjacent to you " +
            "as spaces with spawn points; Zombie tokens are not removed at the end of round.";

        /// <summary>The most Zombies Awaken! prints.</summary>
        public const int AwakenPrintedZombieCount = 4;

        public static CardDefinitionId MasterOfPuppets { get; } = Id("master-of-puppets");

        public const string MasterOfPuppetsText =
            "Whenever you would choose up to three times, you may choose up to five times instead.";

        /// <summary>The standing declaration Master of Puppets supplies while it is unlocked.</summary>
        public const string MasterOfPuppetsRuleId = "mortimer-master-of-puppets";

        public const string MasterOfPuppetsRuleKind = "mortimer-choose-up-to-five-times";

        /// <summary>
        /// Milestone 9.8b. Purple, Tier IV: p.16's permanent Ultimate ability, declared rather than
        /// modelled as a modification - it changes no value, permission or object type. The faces it
        /// reaches read p.16's own question (<see cref="SyntheticRosterCapabilities.UltimateUnlockedV1"/>)
        /// behind their extra passes (<see cref="MortimerRepeatedChoice"/>), so only faces printing
        /// "up to three times" change.
        /// </summary>
        public static CardDefinition MasterOfPuppetsDefinition => LazyMasterOfPuppets.Value;

        private static readonly Lazy<CardDefinition> LazyMasterOfPuppets = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                MasterOfPuppets, ContentRevision, "Master of Puppets", default,
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.UltimateCard(),
                Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.PermanentDeclaration(MasterOfPuppetsRuleId, MasterOfPuppetsRuleKind),
                CardSourceContent.Transcribed(MasterOfPuppetsText)));

        /// <summary>
        /// Silver, Basic: Initiative 6, "Basic Defense / Skill" - Defense 3 and a Skill, Radius 4.
        ///
        /// <para><b>A Defense/Skill card</b> (rulebook pp.13-14): "Cards with this icon can be used as
        /// either a Skill card, or as a Defense card ... The card text is applied whether you play this
        /// card as a Skill card, or discard it to defend, as a Defense card." So the card declares two
        /// printed primaries - the Skill, and a Defense 3 that applies the same text at p.14's step 4 -
        /// and no secondary Defense. Milestone 9.8f corrected it: 9.8a composed an ordinary Skill with a
        /// secondary Defense 3, which the dealing's shine tracing found (no rail badge on the face).</para>
        /// </summary>
        public static CardDefinition AwakenDefinition => LazyAwaken.Value;

        private static readonly Lazy<CardDefinition> LazyAwaken = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                Awaken, ContentRevision, "Awaken!", new InitiativeValue(6),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        AwakenSkill(Awaken, "-skill")),
                    // "discard it to defend, as a Defense card": Defense 3, the same text at step 4,
                    // reaching the card's printed Radius 4 - the icon belongs to the primary it shares.
                    new ActionDefinition(
                        PrimaryDefense, ActionKind.Defense, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, 3),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 4)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        AwakenSkill(Awaken, "-defense"),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.Value,
                        CardTextTiming.DefenseAction)
                },
                null, CardSourceContent.Transcribed(AwakenText)));

        /// <summary>
        /// "Place up to 4 Zombie tokens into spawn points in radius, counting spaces adjacent to you":
        /// four optional supply Places, each after p.18's short-supply step and each declaring the
        /// spawn-point-or-adjacent clause.
        /// </summary>
        private static EffectDefinition AwakenSkill(CardDefinitionId card, string suffix)
        {
            var root = new EffectNodeId("awaken-root");
            var steps = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            for (var index = 1; index <= AwakenPrintedZombieCount; index++)
            {
                var reclaim = new EffectNodeId($"awaken-reclaim-{index}");
                var place = new EffectNodeId($"awaken-place-{index}");
                steps.Add(reclaim);
                steps.Add(place);
                nodes.Add(new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    MortimerComponentContent.ReclaimZombieV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    MortimerComponentContent.PlaceZombieV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.SpawnPointOrAdjacentParameterName,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: steps));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + suffix), ContentRevision, root, nodes);
        }

        internal static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        internal static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        internal static CardDefinitionId Id(string card) =>
            new CardDefinitionId(ContentSetId + "-" + card);
    }
}

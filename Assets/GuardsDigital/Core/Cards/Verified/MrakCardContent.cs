using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Mrak the Rockshaper's printed content. Milestone 8.112a starts it with the one shape his set
    /// prints that no earlier hero does: a <b>printed secondary Attack</b>.
    ///
    /// <para><b>What the icon is.</b> Each of his six Red faces carries a crossed-swords value on its
    /// secondary ribbon beside the Defense (and, on the Skills, Movement) icons - Seismic Slam 6,
    /// the other five 7. Rulebook p.9 names only Movement and Defense secondary icons, so the card
    /// defines this one, and it is performed under p.12-14's general Attack with nothing added:
    /// p.9, "The card text must be applied when, and only when, performing the Primary action", so
    /// no card text runs; it prints no Range, so it is non-Ranged, and p.12 says "Non-Ranged Attacks
    /// can only target units adjacent to you (even if the card has a radius icon)" - which Seismic
    /// Slam's own note restates for this icon; its Attack Total is the printed value plus Attack
    /// items (p.12), answered by the ordinary hero Defense or a minion's defeat; and p.13's Clear
    /// may replace it, because Clear replaces "your entire Attack action".</para>
    ///
    /// <para>Nothing here is registered: Mrak sits behind milestone 7.9's gate until 8.112g deals
    /// him.</para>
    /// </summary>
    public static partial class MrakCardContent
    {
        /// <summary>The revision every Mrak definition carries.</summary>
        public const string ContentRevision = "mrak-8-112-rev-1";

        public const string ContentSetId = "mrak";

        /// <summary>The one identity every printed secondary Attack of his uses on its card.</summary>
        public static ActionDefinitionId SecondaryAttackId { get; } =
            new ActionDefinitionId("secondary-attack");

        /// <summary>
        /// The printed secondary Attack: one Attack value, non-Ranged, card text suppressed, and
        /// replaceable - by p.13's Clear, the only replacement rule an Attack has.
        /// </summary>
        public static ActionDefinition SecondaryAttack(int printedAttack) =>
            new ActionDefinition(
                SecondaryAttackId,
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, printedAttack) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true),
                AttackRangeKind.NonRanged,
                default);

        // -------------------------------------------------------------------------------------------
        // Milestone 8.112b. The basic faces.
        // -------------------------------------------------------------------------------------------

        public static CardDefinitionId Fissure { get; } = Id("fissure");
        public static CardDefinitionId StoneGrip { get; } = Id("stone-grip");

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public const string FissureText =
            "Target a unit adjacent to you. After the attack: Place a rock token in each of the " +
            "first three empty spaces in the straight line from you in the direction of the attack.";

        public const string StoneGripText =
            "Target an enemy hero in range; place exactly 3 rock tokens into empty spaces adjacent " +
            "to the target and farthest away from you.";

        /// <summary>Gold, Basic: Initiative 11, Defense 3, Movement 1, Basic Attack 4.</summary>
        public static CardDefinition FissureDefinition { get; } = new CardDefinition(
            Fissure, ContentRevision, "Fissure", new InitiativeValue(11),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    FissureAfterTheAttack(Fissure),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                Movement(1),
                Defense(3)
            },
            null, CardSourceContent.Transcribed(FissureText));

        /// <summary>Silver, Basic: Initiative 12, Defense 4, Basic Skill - Ranged, Range 3.</summary>
        public static CardDefinition StoneGripDefinition { get; } = new CardDefinition(
            StoneGrip, ContentRevision, "Stone Grip", new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    StoneGripSkill(StoneGrip)),
                Defense(4)
            },
            null, CardSourceContent.Transcribed(StoneGripText));

        /// <summary>
        /// "After the attack: Place a rock token in each of the first three empty spaces in the
        /// straight line from you in the direction of the attack." The attacked space, not the target:
        /// p.14's step 6 may have removed it (MR-Q2 walks on through it when it is empty).
        /// </summary>
        private static EffectDefinition FissureAfterTheAttack(CardDefinitionId card)
        {
            var root = new EffectNodeId("fissure-root");
            var targetSpace = new EffectNodeId("fissure-target-space");
            var actorSpace = new EffectNodeId("fissure-actor-space");
            var reclaim = new EffectNodeId("fissure-reclaim");
            var place = new EffectNodeId("fissure-place");
            var line = new[]
            {
                EffectInputBinding.FromNodeOutput(
                    MrakRockPlacementContent.OriginParameterName, actorSpace, "space",
                    EffectValueKind.Coordinate),
                EffectInputBinding.FromNodeOutput(
                    MrakRockPlacementContent.ThroughParameterName, targetSpace, "space",
                    EffectValueKind.Coordinate)
            };
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { targetSpace, actorSpace, reclaim, place }),
                    new EffectNodeDefinition(
                        targetSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.FormerTargetSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        reclaim, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        MrakRockPlacementContent.ReclaimForLineV1, inputs: line),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        MrakRockPlacementContent.PlaceAlongLineV1, inputs: line,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                MrakRockPlacementContent.PlacedCountOutput, EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// "Target an enemy hero in range; place exactly 3 rock tokens into empty spaces adjacent to
        /// the target and farthest away from you." The printed-Range hero selection (p.9, p.16's
        /// items, immunity), then p.18's short supply, then the farthest spaces (MR-Q3).
        /// </summary>
        private static EffectDefinition StoneGripSkill(CardDefinitionId card)
        {
            var root = new EffectNodeId("stone-grip-root");
            var target = new EffectNodeId("stone-grip-target");
            var reclaim = new EffectNodeId("stone-grip-reclaim");
            var place = new EffectNodeId("stone-grip-place");
            var hero = new[]
            {
                EffectInputBinding.FromNodeOutput(
                    MrakRockPlacementContent.TargetParameterName, target, "chosenHero",
                    EffectValueKind.Hero)
            };
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, reclaim, place }),
                    new EffectNodeDefinition(
                        target, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "relation", SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[] { new EffectOutputBinding("chosenHero", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(
                        reclaim, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        MrakRockPlacementContent.ReclaimForFarthestAdjacentV1, inputs: hero),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        MrakRockPlacementContent.PlaceFarthestAdjacentV1, inputs: hero,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                MrakRockPlacementContent.PlacedCountOutput, EffectValueKind.Integer)
                        })
                });
        }

        private static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        private static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        internal static CardDefinitionId Id(string card) =>
            new CardDefinitionId(ContentSetId + "-" + card);
    }
}

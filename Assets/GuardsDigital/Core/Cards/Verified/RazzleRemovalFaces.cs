using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21e. Razzle's four removal faces - the Red Attacks Phantom Strike, Hit and Gone and
    /// Into Thin Air, and the Silver Defense/Skill Crowd Control - each composed over one generic
    /// removal of the actor's own bodies (<see cref="SyntheticHeroBodyCapabilities.RemoveActorBodiesHandler"/>),
    /// which is p.19's Remove and never a Defeat (RZ-Q6). Nothing here is registered until 9.21j.
    /// </summary>
    public static partial class RazzleCardContent
    {
        public static CardDefinitionId PhantomStrike { get; } = Id("phantom-strike");
        public static CardDefinitionId HitAndGone { get; } = Id("hit-and-gone");
        public static CardDefinitionId IntoThinAir { get; } = Id("into-thin-air");
        public static CardDefinitionId CrowdControl { get; } = Id("crowd-control");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>Crowd Control's Defense, a PRIMARY: the face prints "Basic Defense/Skill" (p.14).</summary>
        public static ActionDefinitionId PrimaryDefense { get; } = new ActionDefinitionId("primary-defense");

        /// <summary>
        /// 9.21e's block list (read by the corpus sweeps until 9.21j mints the aggregate): the three
        /// removal Attacks and Crowd Control.
        /// </summary>
        public static IReadOnlyList<CardDefinition> RemovalDefinitions =>
            new[]
            {
                PhantomStrikeDefinition, HitAndGoneDefinition, IntoThinAirDefinition, CrowdControlDefinition
            };

        public const string PhantomStrikeText =
            "Target a unit adjacent to you. After the attack: If there is more than one of you in play, " +
            "you may remove one of you.";

        public const string HitAndGoneText =
            "Target a unit adjacent to you. After the attack: You may remove one or more of you in " +
            "play, except the last one of you.";

        public const string IntoThinAirText =
            "Target a unit adjacent to you. After the attack: You may remove one or more of you in " +
            "play, including all of you. (If all are removed, you are not defeated. Respawn as normal ).";

        public const string CrowdControlText =
            "When used as a defense action, +2 [Defense] Defense for each other you in radius. When " +
            "used as a skill action, remove all other you in play.";

        /// <summary>Red, Tier I: Initiative 9, Defense 3, Movement 4, Attack 4.</summary>
        public static CardDefinition PhantomStrikeDefinition => LazyPhantomStrike.Value;

        private static readonly Lazy<CardDefinition> LazyPhantomStrike = new Lazy<CardDefinition>(() =>
            RemovalAttack(PhantomStrike, "Phantom Strike", CardTier.I, 9, 3, 4, PrintedValueChannel.None,
                new[] { HitAndGone, Id("rummage") },
                RazzleEffectContent.PhantomStrikeAfterTheAttack(PhantomStrike, ContentRevision),
                PhantomStrikeText));

        /// <summary>Red, Tier II: Initiative 9, Defense 4, Movement 4, Attack 4, Defense item.</summary>
        public static CardDefinition HitAndGoneDefinition => LazyHitAndGone.Value;

        private static readonly Lazy<CardDefinition> LazyHitAndGone = new Lazy<CardDefinition>(() =>
            RemovalAttack(HitAndGone, "Hit and Gone", CardTier.II, 9, 4, 4, PrintedValueChannel.Defense,
                new[] { IntoThinAir, Id("ransack") },
                RazzleEffectContent.RemoveAfterTheAttack(
                    HitAndGone, ContentRevision, ActorBodyRemoval.OneOrMoreButNotTheLast),
                HitAndGoneText));

        /// <summary>Red, Tier III: Initiative 10, Defense 4, Movement 4, Attack 5, Range item.</summary>
        public static CardDefinition IntoThinAirDefinition => LazyIntoThinAir.Value;

        private static readonly Lazy<CardDefinition> LazyIntoThinAir = new Lazy<CardDefinition>(() =>
            RemovalAttack(IntoThinAir, "Into Thin Air", CardTier.III, 10, 4, 5, PrintedValueChannel.Range,
                Array.Empty<CardDefinitionId>(),
                RazzleEffectContent.RemoveAfterTheAttack(
                    IntoThinAir, ContentRevision, ActorBodyRemoval.OneOrMoreIncludingAll),
                IntoThinAirText));

        /// <summary>
        /// Silver, Basic: Initiative 13, "Basic Defense/Skill" - Defense 1 and a Skill, Radius 3, no
        /// ribbon. p.14's Defense/Skill card declares two printed primaries, as Mortimer's Awaken! does.
        /// Its text is split by how the card is used, so each half composes onto its own primary only:
        /// the Skill carries "remove all other you in play" (greyed with no other body, RZ-Q7); the
        /// Defense carries "+2 Defense for each other you in radius", composed by 9.21g as
        /// <see cref="CrowdControlDefenseText"/>.
        /// </summary>
        public static CardDefinition CrowdControlDefinition => LazyCrowdControl.Value;

        private static readonly Lazy<CardDefinition> LazyCrowdControl = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                CrowdControl, ContentRevision, "Crowd Control", new InitiativeValue(13),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        RazzleEffectContent.CrowdControlSkill(CrowdControl, ContentRevision)),
                    new ActionDefinition(
                        PrimaryDefense, ActionKind.Defense, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, 1),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 3)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        // ===== 9.21g: the Defense text (RazzleCountingFaces.cs) =====
                        rosterValueContributions: CrowdControlDefenseText)
                },
                null, CardSourceContent.Transcribed(CrowdControlText)));

        /// <summary>
        /// The three Red faces' one printed shape: p.13's non-Ranged Attack on a unit adjacent to the
        /// acting body, then the after-the-attack removal, Movement 4 and the printed Defense.
        /// </summary>
        private static CardDefinition RemovalAttack(
            CardDefinitionId card, string name, CardTier tier, int initiative, int defense, int attack,
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
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, attack) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        text,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                    Movement(4),
                    Defense(defense)
                },
                null, CardSourceContent.Transcribed(printed));
    }

    public static partial class RazzleEffectContent
    {
        /// <summary>
        /// Milestone 9.21e. "Remove ... of you": the actor's own bodies the closed input admits leave
        /// the board, back to his supply. Never a Defeat (RZ-Q6).
        /// </summary>
        public static EffectCapabilityReference RemoveBodiesV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-remove-bodies"), 1);

        /// <summary>Milestone 9.21e. "If there is more than one of you in play": a read-only predicate.</summary>
        public static EffectCapabilityReference SeveralBodiesV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("razzle-several-bodies"), 1);

        /// <summary>The removal node of every face, so a paused removal names its face.</summary>
        public static string RemovalNode(CardDefinitionId card) => card.Value + "-remove";

        /// <summary>Phantom Strike's printed "If" gate.</summary>
        public static string GateNode(CardDefinitionId card) => card.Value + "-gate";

        /// <summary>
        /// Phantom Strike: "After the attack: If there is more than one of you in play, you may remove
        /// one of you." A Conditional over the printed "If" with no else-child - a false gate is RULING
        /// 53's legal do-nothing - whose then-child is an optional removal of exactly one body, the
        /// acting body included (RZ-Q6 "one of you").
        /// </summary>
        public static EffectDefinition PhantomStrikeAfterTheAttack(CardDefinitionId card, string contentRevision)
        {
            var gate = new EffectNodeId(GateNode(card));
            var remove = new EffectNodeId(RemovalNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), contentRevision, gate,
                new[]
                {
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory, SeveralBodiesV1,
                        childNodeIds: new[] { remove },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticHeroBodyCapabilities.SeveralBodiesOutput, EffectValueKind.Boolean)
                        }),
                    Removal(remove, EffectFailurePolicy.May, ActorBodyRemoval.OneBody)
                });
        }

        /// <summary>
        /// Hit and Gone ("You may remove one or more of you in play, except the last one of you") and
        /// Into Thin Air ("... including all of you"): one optional removal whose bounds are the printed
        /// quantity. Into Thin Air's reminder "(If all are removed, you are not defeated. Respawn as
        /// normal)" composes to no node: removal is never a Defeat, and p.7's respawn already keys on
        /// the hero being off the board.
        /// </summary>
        public static EffectDefinition RemoveAfterTheAttack(
            CardDefinitionId card, string contentRevision, ActorBodyRemoval removal)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var remove = new EffectNodeId(RemovalNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { remove }),
                    Removal(remove, EffectFailurePolicy.May, removal)
                });
        }

        /// <summary>
        /// Crowd Control's Skill half: "When used as a skill action, remove all other you in play." A
        /// mandatory removal of every body but the acting one; with no other body it has nothing to act
        /// on, so the Skill is greyed (Rule 14, RZ-Q7 - RULING 53 covers only a printed "If").
        /// </summary>
        public static EffectDefinition CrowdControlSkill(CardDefinitionId card, string contentRevision)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var remove = new EffectNodeId(RemovalNode(card));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"), contentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { remove }),
                    Removal(remove, EffectFailurePolicy.Mandatory, ActorBodyRemoval.EveryOtherBody)
                });
        }

        private static EffectNodeDefinition Removal(
            EffectNodeId id, EffectFailurePolicy policy, ActorBodyRemoval removal) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Capability, policy, RemoveBodiesV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticHeroBodyCapabilities.RemovalParameterName, removal)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticHeroBodyCapabilities.RemovedOutput, EffectValueKind.Integer)
                });
    }
}

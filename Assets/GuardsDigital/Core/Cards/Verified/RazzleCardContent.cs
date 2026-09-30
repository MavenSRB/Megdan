using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Razzle the Ringmaster's printed content. Milestone 9.21d starts it with his Gold basic and the
    /// boxed Handicap that prints the same text.
    ///
    /// <para><b>Stunt Doubles</b> (Gold, Basic): Initiative 13, Defense 1, Movement 1, Basic Attack 2,
    /// Radius 1. "Target a unit adjacent to you. After the attack: Spawn up to 3 more of you in radius;
    /// each of you is the same hero, except when actions are performed. If defeated, remove all of
    /// you." p.13's non-Ranged Attack, then <see cref="RazzleEffectContent.SpawnAfterTheAttack"/>.</para>
    ///
    /// <para><b>Stunted Doubles</b> (Gold, p.20 Handicap, boxed at setup): Initiative 12, Defense 1,
    /// Movement 1, Basic Attack 1, Radius 1, the same text with "Target a hero adjacent to you." -
    /// the one narrowing <see cref="AttackTargetableUnits.HeroesOnly"/> names. Composed, never dealt.</para>
    ///
    /// <para>Registered and dealt at 9.21j (<see cref="AllDefinitions"/>).</para>
    /// </summary>
    public static partial class RazzleCardContent
    {
        // ===== MERGE BLOCK =====
        // The set's shared identities. The aggregate (AllDefinitions, DealtDefinitions) is 9.21j's,
        // in RazzleCardContent.Set.cs; it replaced 9.21d-9.21h's per-block lists.

        /// <summary>
        /// The revision every Razzle definition carries: 9.21d-9.21h composed under
        /// <c>razzle-9-21d-prep</c>; milestone 9.21j deals him under his first dealt revision.
        /// </summary>
        public const string ContentRevision = "razzle-9-21j-rev-1";

        public const string ContentSetId = "razzle";

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

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
        // ===== END MERGE BLOCK =====

        public static CardDefinitionId StuntDoubles { get; } = Id("stunt-doubles");
        public static CardDefinitionId StuntedDoubles { get; } = Id("stunted-doubles");


        public const string StuntDoublesText =
            "Target a unit adjacent to you. After the attack: Spawn up to 3 more of you in radius; " +
            "each of you is the same hero, except when actions are performed. If defeated, remove " +
            "all of you.";

        public const string StuntedDoublesText =
            "Target a hero adjacent to you. After the attack: Spawn up to 3 more of you in radius; " +
            "each of you is the same hero, except when actions are performed. If defeated, remove " +
            "all of you.";

        /// <summary>Gold, Basic: Initiative 13, Defense 1, Movement 1, Basic Attack 2, Radius 1.</summary>
        public static CardDefinition StuntDoublesDefinition => LazyStuntDoubles.Value;

        private static readonly Lazy<CardDefinition> LazyStuntDoubles = new Lazy<CardDefinition>(() =>
            Doubles(StuntDoubles, "Stunt Doubles", 13, 2, AttackTargetableUnits.HeroesAndMinions,
                StuntDoublesText));

        /// <summary>
        /// Gold, p.20 Handicap: Initiative 12, Defense 1, Movement 1, Basic Attack 1, Radius 1, "Target
        /// a hero". Boxed at setup (p.20), so it is composed and never dealt.
        /// </summary>
        public static CardDefinition StuntedDoublesDefinition => LazyStuntedDoubles.Value;

        private static readonly Lazy<CardDefinition> LazyStuntedDoubles = new Lazy<CardDefinition>(() =>
            Doubles(StuntedDoubles, "Stunted Doubles", 12, 1, AttackTargetableUnits.HeroesOnly,
                StuntedDoublesText));

        /// <summary>
        /// The pair's one printed shape: a non-Ranged Basic Attack whose Radius icon bounds the spawn
        /// (p.9; p.12 "Non-Ranged Attacks can only target units adjacent to you (even if the card has a
        /// radius icon)"), the after-the-attack spawn, Movement 1 and Defense 1. Only the Initiative,
        /// the Attack value and the printed noun of the target differ.
        /// </summary>
        private static CardDefinition Doubles(
            CardDefinitionId card, string name, int initiative, int attack,
            AttackTargetableUnits targets, string text)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged);
            return new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, 1)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged, default,
                        RazzleEffectContent.SpawnAfterTheAttack(card, ContentRevision),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack,
                        targets == AttackTargetableUnits.HeroesAndMinions
                            ? classification
                            : new PrintedAttackTargetPolicy(
                                targets, classification.Reach, classification.Line)),
                    Movement(1),
                    Defense(1)
                },
                null, CardSourceContent.Transcribed(text));
        }
    }
}

using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Trinkets' five Red faces, composed but not dealt until the complete set exists.</summary>
    public static partial class TrinketsCardContent
    {
        public const string ContentRevision = "trinkets-9-39-rev-1";

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static CardDefinitionId MakeshiftMinigun { get; } =
            new CardDefinitionId("trinkets-makeshift-minigun");
        public static CardDefinitionId GatlingGun { get; } =
            new CardDefinitionId("trinkets-gatling-gun");
        public static CardDefinitionId SteamDischarge { get; } =
            new CardDefinitionId("trinkets-steam-discharge");
        public static CardDefinitionId SuperchargedCannon { get; } =
            new CardDefinitionId("trinkets-supercharged-cannon");
        public static CardDefinitionId FlameBelcher { get; } =
            new CardDefinitionId("trinkets-flame-belcher");

        public const string CannonText =
            "Target a unit in range of both you and the Turret. If the target is in a straight " +
            "line from you, and in a straight line from the Turret, gain +2 Attack.";
        public const string SuperchargedText =
            "Target a unit in range of both you and the Turret. If the target is in a straight " +
            "line from you, and in a straight line from the Turret, gain +3 Attack.";
        public const string SteamText =
            "Target a unit in range adjacent to the Turret. May repeat once on a different enemy unit.";
        public const string FlameText =
            "Target a unit in range adjacent to the Turret. May repeat up to two times on different enemy units.";

        public static IReadOnlyList<CardDefinition> RedDefinitions => LazyReds.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyReds =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Build(MakeshiftMinigun, "Makeshift Minigun", CardTier.I,
                    8, 5, 4, 3, 2, PrintedValueChannel.None, 2, 0,
                    CannonText, new[] { GatlingGun, SteamDischarge }),
                Build(GatlingGun, "Gatling Gun", CardTier.II,
                    9, 6, 4, 3, 3, PrintedValueChannel.Defense, 2, 0,
                    CannonText, new[] { SuperchargedCannon, FlameBelcher }),
                Build(SteamDischarge, "Steam Discharge", CardTier.II,
                    9, 6, 4, 4, 4, PrintedValueChannel.Initiative, 0, 1,
                    SteamText, new[] { SuperchargedCannon, FlameBelcher }),
                Build(SuperchargedCannon, "Supercharged Cannon", CardTier.III,
                    9, 6, 4, 4, 3, PrintedValueChannel.Defense, 3, 0,
                    SuperchargedText, Array.Empty<CardDefinitionId>()),
                Build(FlameBelcher, "Flame Belcher", CardTier.III,
                    9, 6, 4, 5, 4, PrintedValueChannel.Radius, 0, 2,
                    FlameText, Array.Empty<CardDefinitionId>())
            }));

        private static CardDefinition Build(
            CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int movement, int attack, int range,
            PrintedValueChannel itemIcon, int lineBonus, int differentUnitRepeats,
            string text, IReadOnlyList<CardDefinitionId> next) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, itemIcon, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryRangedAttack, ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Range, range)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        TrinketsEffectContent.RedAttack(
                            id, ContentRevision, lineBonus > 0),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                            .BoundByCardText(),
                        lineBonus > 0
                            ? new[]
                            {
                                new RosterValueContributionDefinition(
                                    PrintedValueChannel.Attack,
                                    RosterValueQueryKind
                                        .AttackTargetInStraightLineFromActorAndOwnedObstacle,
                                    lineBonus,
                                    TrinketsComponentContent.TurretKind)
                            }
                            : null,
                        secondCardTextRoot: TrinketsEffectContent.RedRepeat(
                            id, ContentRevision, differentUnitRepeats),
                        secondCardTextTiming: differentUnitRepeats > 0
                            ? CardTextTiming.AfterTheAttack
                            : CardTextTiming.None),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(text));
    }
}

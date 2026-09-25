using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Source-verified Gold basic and five Red upgrades, staged until the complete Snorri
    /// set is dealt. Their effect roots share the live current-turn rune authority.
    /// </summary>
    public static class SnorriAttackFaces
    {
        public const string ContentRevision = "snorri-8-93b-rev-1";

        public static CardDefinitionId RuneSigils { get; } =
            new CardDefinitionId("snorri-rune-sigils");
        public static CardDefinitionId RunicDagger { get; } =
            new CardDefinitionId("snorri-runic-dagger");
        public static CardDefinitionId RunicHammer { get; } =
            new CardDefinitionId("snorri-runic-hammer");
        public static CardDefinitionId Runecaster { get; } =
            new CardDefinitionId("snorri-runecaster");
        public static CardDefinitionId RunicBattleaxe { get; } =
            new CardDefinitionId("snorri-runic-battleaxe");
        public static CardDefinitionId Runeblaster { get; } =
            new CardDefinitionId("snorri-runeblaster");

        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Build(RuneSigils, "Rune Sigils", CardTier.None, 11, 2, 1, 2, 2,
                    PrintedValueChannel.None,
                    "Target a unit adjacent to you. If a rune is active: " +
                    "Rune 1 — You may target a minion in range instead. " +
                    "Rune 2 — +3 Attack. " +
                    "Rune 3 — If you target a hero, gain 3 coins. " +
                    "Rune 4 — Repeat once on a different hero in range.",
                    ranged: true, sigils: true),
                Build(RunicDagger, "Runic Dagger", CardTier.I, 7, 6, 3, 5, 0,
                    PrintedValueChannel.None,
                    "Target a unit adjacent to you. After the attack: If the Rune 3 rune is " +
                    "active, you may retrieve a discarded card.",
                    next: new[] { RunicHammer, Runecaster }, retrieve: true),
                Build(RunicHammer, "Runic Hammer", CardTier.II, 7, 7, 3, 5, 0,
                    PrintedValueChannel.Initiative,
                    "Before the attack: If the Rune 4 rune is active, you may move 1 space. " +
                    "Target a unit adjacent to you. After the attack: If the Rune 3 rune is " +
                    "active, you may retrieve a discarded card.",
                    next: new[] { RunicBattleaxe, Runeblaster },
                    moveBefore: true, retrieve: true),
                Build(Runecaster, "Runecaster", CardTier.II, 7, 6, 3, 4, 3,
                    PrintedValueChannel.Defense,
                    "Target a unit at maximum range. After the attack: If a rune is active: " +
                    "Rune 4 — Move up to 2 spaces. Rune 2 — An enemy hero adjacent to the " +
                    "target's space discards a card, or is defeated.",
                    next: new[] { RunicBattleaxe, Runeblaster }, ranged: true,
                    maximumRange: true, rangedRiders: true),
                Build(RunicBattleaxe, "Runic Battleaxe", CardTier.III, 8, 7, 3, 6, 0,
                    PrintedValueChannel.Radius,
                    "Before the attack: If the Rune 4 rune is active, you may move 1 space. " +
                    "Target a unit adjacent to you. After the attack: If a rune is active: " +
                    "Rune 2 — May repeat once on an enemy minion. " +
                    "Rune 3 — You may retrieve a discarded card.",
                    moveBefore: true, retrieve: true, battleaxe: true),
                Build(Runeblaster, "Runeblaster", CardTier.III, 8, 6, 3, 5, 3,
                    PrintedValueChannel.Movement,
                    "If the Rune 1 rune is active, target a unit in range, otherwise target " +
                    "a unit at maximum range. After the attack: If a rune is active: " +
                    "Rune 4 — Move up to 2 spaces. Rune 2 — An enemy hero adjacent to the " +
                    "target's space discards a card, or is defeated.",
                    ranged: true, blaster: true, rangedRiders: true)
            }));

        private static CardDefinition Build(
            CardDefinitionId id, string name, CardTier tier, int initiative, int defense,
            int movement, int attack, int range, PrintedValueChannel item,
            string text, IReadOnlyList<CardDefinitionId> next = null,
            bool ranged = false, bool sigils = false, bool moveBefore = false,
            bool maximumRange = false, bool blaster = false,
            bool retrieve = false, bool battleaxe = false, bool rangedRiders = false)
        {
            var before = SnorriAttackEffectContent.BeforeAttack(
                id, ContentRevision, moveBefore, sigils, blaster);
            var after = SnorriAttackEffectContent.AfterAttack(
                id, ContentRevision, sigils, retrieve, battleaxe, rangedRiders);
            var classification = ranged ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged;
            var policy = PrintedAttackTargetPolicy.ForClassification(classification);
            if (before != null)
                policy = policy.BoundByCardText();
            else if (maximumRange)
                policy = policy.AtExactlyThePrintedRange();

            var printed = new List<PrintedValueDefinition>
            {
                new PrintedValueDefinition(PrintedValueChannel.Attack, attack)
            };
            if (ranged)
                printed.Add(new PrintedValueDefinition(PrintedValueChannel.Range, range));

            var progression = tier == CardTier.None
                ? CardProgressionProfile.BasicCard(CardColor.Gold)
                : CardProgressionProfile.NonBasicCard(
                    CardColor.Red, tier, item, next ?? Array.Empty<CardDefinitionId>());
            return new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial, progression,
                new[]
                {
                    new ActionDefinition(
                        ranged ? PrimaryRangedAttack : PrimaryAttack,
                        ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        printed,
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true),
                        classification, default,
                        before ?? after,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        before != null
                            ? CardTextTiming.BeforeTheAttack
                            : CardTextTiming.AfterTheAttack,
                        policy,
                        sigils
                            ? new[]
                            {
                                new RosterValueContributionDefinition(
                                    PrintedValueChannel.Attack,
                                    RosterValueQueryKind.ActiveTurnSlotMarker, 3,
                                    markerDefinition:
                                        SnorriComponentContent.Runes[1].Reference)
                            }
                            : null,
                        secondCardTextRoot: before != null ? after : null,
                        secondCardTextTiming: before != null && after != null
                            ? CardTextTiming.AfterTheAttack
                            : CardTextTiming.None),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Movement, movement)
                        },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, defense)
                        },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                null, CardSourceContent.Transcribed(text));
        }
    }
}

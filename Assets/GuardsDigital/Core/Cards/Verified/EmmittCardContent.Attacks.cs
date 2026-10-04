using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string ReverseTimeText =
            "Target a unit adjacent to you. After the attack, Next turn: Heroes with lower initiative " +
            "act before heroes with higher initiative; this effect ignores immunity. " +
            "(Resolve the Ties as normal.)";

        public const string TemporalPunchText =
            "Target a unit adjacent to you; when defending, the enemy hero must use the Initiative " +
            "value of their card and items instead of the Defense value of their card and items. " +
            "(Minion defense modifiers are applied as normal.)";

        public const string TemporalSlamText =
            "Target a unit adjacent to you; when defending, the enemy hero must use the Initiative " +
            "value of their card and items instead of the Defense value of their card and items.";

        public const string TemporalJudgementText = TemporalSlamText;

        public static CardDefinition ReverseTimeDefinition { get; } = new CardDefinition(
            ReverseTime, ContentRevision, "Reverse Time", new InitiativeValue(11),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    GrantEffect(ReverseTime, EmmittActiveEffectContent.ReverseTimeV1),
                    PrintedTextComposition.None, PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                Movement(1), Defense(2)
            },
            null, CardSourceContent.Transcribed(ReverseTimeText));

        public static CardDefinition TemporalPunchDefinition { get; } = RedPunch(
            TemporalPunch, "Temporal Punch", CardTier.I, 7, 7, 9,
            PrintedValueChannel.None, new[] { TemporalSlam, Flashback }, TemporalPunchText);

        public static CardDefinition TemporalSlamDefinition { get; } = RedPunch(
            TemporalSlam, "Temporal Slam", CardTier.II, 7, 7, 11,
            PrintedValueChannel.Initiative, new[] { TemporalJudgement, DejaVu }, TemporalSlamText);

        public static CardDefinition TemporalJudgementDefinition { get; } = RedPunch(
            TemporalJudgement, "Temporal Judgement", CardTier.III, 8, 8, 12,
            PrintedValueChannel.Range, Array.Empty<CardDefinitionId>(), TemporalJudgementText);

        private static CardDefinition RedPunch(CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int attack, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> upgrades, string text) => new CardDefinition(
            id, ContentRevision, name, new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, upgrades),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, attack) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    GrantEffect(id, EmmittActiveEffectContent.TemporalPunchV1),
                    PrintedTextComposition.None, PrintedDefenseResolution.None,
                    CardTextTiming.BeforeTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                Movement(3), Defense(defense)
            },
            null, CardSourceContent.Transcribed(text));
    }
}

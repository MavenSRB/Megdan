using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Snorri's staged Silver face. Registration with the dealt roster follows only when
    /// all printed faces have been composed.
    /// </summary>
    public static class SnorriSilverFace
    {
        public const string ContentRevision = "snorri-8-93a-rev-1";
        public static CardDefinitionId InscribeTheRunes { get; } =
            new CardDefinitionId("snorri-inscribe-the-runes");
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static CardDefinition Silver { get; } = new CardDefinition(
            InscribeTheRunes, ContentRevision, "Inscribe the Runes",
            new InitiativeValue(1), CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(PrimarySkill, ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    new EffectDefinition(
                        new EffectDefinitionId("snorri-inscribe-the-runes-skill"),
                        ContentRevision, new EffectNodeId("root"),
                        new[]
                        {
                            new EffectNodeDefinition(new EffectNodeId("root"),
                                EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                                SnorriComponentContent.InscribeRunesV1)
                        })),
                new ActionDefinition(SecondaryDefense, ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            }, null, CardSourceContent.Transcribed(
                "Give yourself 4 rune markers and place them below each one of your turn slots. " +
                "A rune is active as long as it is below the current turn slot. " +
                "Rune markers are not removed at the end of round, nor if you are defeated."));
    }
}

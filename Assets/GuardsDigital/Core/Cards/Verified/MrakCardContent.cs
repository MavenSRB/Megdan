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
    public static class MrakCardContent
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
    }
}

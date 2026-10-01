using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Gydion the Archwizard's printed content - the twenty-ninth hero, dealt at milestone 9.30i.
    ///
    /// <para><b>His twenty-two spell cards</b> (<see cref="SpellDefinitions"/>, 9.30e) are read from the
    /// shard's Spells table and checked against <c>cards/other-cards/spells-tt*__front.png</c>: name,
    /// colour, Tier and classification (9.30b's <see cref="CardProgressionProfile.SpellCard"/>), the
    /// printed primary's type and its Attack, Range, Radius and Movement values; a printed <c>0+</c> is
    /// a printed 0 its own text raises. Each is composed on 9.30c's cast path: the four counting spells
    /// and the Familiar (9.30e), the twelve that reuse existing contracts (9.30d), the immunity,
    /// discard, Life and forced-move spells (9.30f) and Wish (9.30g).</para>
    ///
    /// <para><b>His nineteen action fronts</b> (<see cref="ActionCardDefinitions"/>): Prepare Spells and
    /// The Archwizard (9.30g, <c>GydionCardContent.Archwizard.cs</c>), and Cantrip, the fifteen coloured
    /// cards and the boxed Cheap Trick (9.30i, <c>GydionCardContent.Actions.cs</c>). The set, the p.6
    /// hand and the book are <c>GydionCardContent.Set.cs</c>; the registration is
    /// <c>MatchContentCatalogs</c>'s, milestone 7.9's gate.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        // ===== MERGE BLOCK (9.30e; 9.30d, 9.30f, 9.30g and 9.30i extended) =====
        // The set's shared identities. The aggregate (AllDefinitions, DealtDefinitions) is 9.30i's,
        // in GydionCardContent.Set.cs.

        /// <summary>
        /// The revision every Gydion definition carries: 9.30d-9.30h composed under
        /// <c>gydion-9-30-prep</c>; milestone 9.30i deals him under his first dealt revision.
        /// </summary>
        public const string ContentRevision = "gydion-9-30i-rev-1";

        /// <summary>p.5's content-set identity. Every id is prefixed with it.</summary>
        public const string ContentSetId = "gydion";

        /// <summary>A card's one printed primary, by its printed type.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimaryMovement { get; } = new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        internal static CardDefinitionId Id(string card) => new CardDefinitionId(ContentSetId + "-" + card);
        // ===== END MERGE BLOCK =====

        private static PrintedValueDefinition Value(PrintedValueChannel channel, int amount) =>
            new PrintedValueDefinition(channel, amount);
    }
}

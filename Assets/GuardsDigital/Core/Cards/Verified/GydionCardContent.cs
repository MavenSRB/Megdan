using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Gydion the Archwizard's printed content - the twenty-ninth hero. Milestone 9.30e starts it with
    /// his TWENTY-TWO SPELL CARDS (<see cref="SpellDefinitions"/>), read from the shard's Spells table
    /// and checked against <c>cards/other-cards/spells-tt*__front.png</c>: name, colour, Tier and
    /// classification (9.30b's <see cref="CardProgressionProfile.SpellCard"/>), the printed primary's
    /// type and its Attack, Range, Radius and Movement values. A printed <c>0+</c> is a printed 0 its
    /// own text raises.
    ///
    /// <para><b>Four spells are composed here</b> (<c>GydionCountingSpells.cs</c>): Sunburst,
    /// Dimension Door, Midas' Touch and Find Familiar. The other eighteen carry their transcribed
    /// text and <see cref="PrintedTextComposition.AwaitedAt"/> the block that owes it - 9.30d the
    /// twelve that reuse existing contracts, 9.30f the five immunity, discard, Life and forced-move
    /// spells, 9.30g Wish - so a cast of one is refused rather than performed short
    /// (<see cref="BorrowedPrimaryActionDeclaration.Refuses"/>, 8.4's honesty rule).</para>
    ///
    /// <para><b>His nineteen action fronts are not here yet</b>, and nothing is registered or dealt:
    /// <c>MatchContentCatalogs</c> names no Gydion content until 9.30i, milestone 7.9's gate.
    /// <see cref="AllDefinitions"/> is the spells today; the dealing adds the fronts.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        // ===== MERGE BLOCK (9.30e; 9.30d, 9.30f, 9.30g and 9.30i extend) =====
        // The set's shared identities. A later block composes a spell by replacing its row in
        // GydionCardContent.Spells.cs (the `Owed(...)` primary) with its own builder, under this one
        // revision; 9.30i mints the dealt revision and the fronts' aggregate.

        /// <summary>
        /// The revision every Gydion definition carries while his set is composed (9.30d-9.30h);
        /// milestone 9.30i deals him under his first dealt revision.
        /// </summary>
        public const string ContentRevision = "gydion-9-30-prep";

        /// <summary>p.5's content-set identity. Every id is prefixed with it.</summary>
        public const string ContentSetId = "gydion";

        /// <summary>A spell's one printed primary, by its printed type.</summary>
        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimaryMovement { get; } = new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        internal static CardDefinitionId Id(string card) => new CardDefinitionId(ContentSetId + "-" + card);

        /// <summary>
        /// Every Gydion definition this build composes - the twenty-two spells today (9.30e). The
        /// reflection sweeps read it (milestone 9.8a's trap: a <c>*CardContent</c> class declares it).
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => SpellDefinitions;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Gydion prints no card '{id}'.", nameof(id));
        // ===== END MERGE BLOCK =====

        /// <summary>
        /// A spell's printed primary whose text a later block composes. The action is complete as
        /// inventory - kind, values, classification - and owes only its text.
        /// </summary>
        private static ActionDefinition Owed(
            ActionKind kind, AttackRangeKind range, string owingMilestone,
            params PrintedValueDefinition[] values) =>
            new ActionDefinition(
                PrimaryFor(kind, range), kind, ActionDefinitionOrigin.PrintedPrimary, values,
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                range, default, null, PrintedTextComposition.AwaitedAt(owingMilestone));

        private static ActionDefinitionId PrimaryFor(ActionKind kind, AttackRangeKind range)
        {
            switch (kind)
            {
                case ActionKind.Attack:
                    return range == AttackRangeKind.Ranged ? PrimaryRangedAttack : PrimaryAttack;
                case ActionKind.Movement:
                    return PrimaryMovement;
                case ActionKind.Skill:
                    return PrimarySkill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "A spell prints an Attack, a Movement or a Skill.");
            }
        }

        private static PrintedValueDefinition Value(PrintedValueChannel channel, int amount) =>
            new PrintedValueDefinition(channel, amount);
    }
}

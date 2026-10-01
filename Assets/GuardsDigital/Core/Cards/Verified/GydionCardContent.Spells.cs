using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30e. Gydion's twenty-two spell cards - the shard's Spells table, each row checked
    /// against its face (<c>cards/other-cards/spells-tt*__front.png</c>, 372x260 landscape). A spell
    /// prints no Initiative and no Item icon (9.30b), so a row is its name, colour, Tier, one printed
    /// primary and its words.
    ///
    /// <para><b>The identities are stable and the art keys on them</b> (9.30h): <c>gydion-</c> and the
    /// printed name, lower-case and hyphenated, apostrophe dropped (<c>gydion-midas-touch</c>). Each
    /// spell's naming step (9.30c's <see cref="SyntheticSpellbookCapabilities.SpellInBookHandler"/>) is
    /// <see cref="InBookV1"/>: the spell's id and <c>-in-book</c>.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        // The twenty-two, in the shard's (Tabletopia's spells-tt..tt22) order.
        public static CardDefinitionId MagicMissile { get; } = Id("magic-missile");
        public static CardDefinitionId ShockingGrasp { get; } = Id("shocking-grasp");
        public static CardDefinitionId ExpeditiousRetreat { get; } = Id("expeditious-retreat");
        public static CardDefinitionId BurningHands { get; } = Id("burning-hands");
        public static CardDefinitionId Fireball { get; } = Id("fireball");
        public static CardDefinitionId Sunburst { get; } = Id("sunburst");
        public static CardDefinitionId VampiricTouch { get; } = Id("vampiric-touch");
        public static CardDefinitionId CreateUndead { get; } = Id("create-undead");
        public static CardDefinitionId EnergyDrain { get; } = Id("energy-drain");
        public static CardDefinitionId Suggestion { get; } = Id("suggestion");
        public static CardDefinitionId DominatePerson { get; } = Id("dominate-person");
        public static CardDefinitionId PowerWordKill { get; } = Id("power-word-kill");
        public static CardDefinitionId MidasTouch { get; } = Id("midas-touch");
        public static CardDefinitionId Disintegrate { get; } = Id("disintegrate");
        public static CardDefinitionId Polymorph { get; } = Id("polymorph");
        public static CardDefinitionId Shield { get; } = Id("shield");
        public static CardDefinitionId Banishment { get; } = Id("banishment");
        public static CardDefinitionId Invulnerability { get; } = Id("invulnerability");
        public static CardDefinitionId FindFamiliar { get; } = Id("find-familiar");
        public static CardDefinitionId DimensionDoor { get; } = Id("dimension-door");
        public static CardDefinitionId CloudKill { get; } = Id("cloud-kill");
        public static CardDefinitionId Wish { get; } = Id("wish");

        /// <summary>The twenty-two spell definitions, in the shard's order (GY-Q1: all in the book).</summary>
        public static IReadOnlyList<CardDefinition> SpellDefinitions => LazySpells.Value;

        /// <summary>
        /// The twenty-two identities in the same order - what a composition that names other spells
        /// reads (Find Familiar's "other spell cards"), since a definition cannot read the table it is
        /// being built into.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> SpellIds => new[]
        {
            MagicMissile, ShockingGrasp, ExpeditiousRetreat, BurningHands, Fireball, Sunburst,
            VampiricTouch, CreateUndead, EnergyDrain, Suggestion, DominatePerson, PowerWordKill,
            MidasTouch, Disintegrate, Polymorph, Shield, Banishment, Invulnerability,
            FindFamiliar, DimensionDoor, CloudKill, Wish
        };

        /// <summary>
        /// The naming step of <paramref name="spell"/> - "the X spell in the spellbook" - which a
        /// cast bullet (<see cref="SyntheticSpellbookCapabilities.CastArm"/>) and a removal option
        /// (<see cref="SyntheticSpellbookCapabilities.RemoveArm"/>) start with.
        /// </summary>
        public static EffectCapabilityReference InBookV1(CardDefinitionId spell)
        {
            if (!spell.IsDefined) throw new ArgumentException("A naming step names its spell.", nameof(spell));
            return new EffectCapabilityReference(new EffectCapabilityId(spell.Value + "-in-book"), 1);
        }

        /// <summary>
        /// The twenty-two naming steps, each bound to its spell's definition. Registered with the
        /// dealing (9.30i); a fixture's catalog carries them until then.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SpellInBookHandlers() =>
            SpellDefinitions.Select(spell =>
                SyntheticSpellbookCapabilities.SpellInBookHandler(InBookV1(spell.Id), spell.Id));

        // ----------------------------------------------------------------------------------------
        // The printed words, from the faces (icons as [Name]).
        // ----------------------------------------------------------------------------------------

        public const string MagicMissileText = "Target a unit in range and not adjacent to you.";

        public const string ShockingGraspText =
            "Target a unit adjacent to you. After the attack: Move the target up to 1 space.";

        public const string ExpeditiousRetreatText = "Move only in a straight line.";

        public const string BurningHandsText =
            "Target a unit adjacent to you. Before the attack: Up to 1 enemy hero adjacent to the " +
            "target discards a card, if able.";

        public const string FireballText =
            "Target a unit in range, not adjacent to you or to your friendly unit.";

        public const string SunburstText =
            "Target a unit at maximum range. +1 [Attack] Attack and +1 [Range] Range for each other " +
            "spell card removed from the spellbook.";

        public const string VampiricTouchText =
            "Target a unit adjacent to you. After the attack: You may retrieve a discarded card.";

        public const string CreateUndeadText =
            "Respawn a friendly minion in an empty friendly spawn point in range in the battle zone.";

        public const string EnergyDrainText =
            "An enemy hero in range discards a non-basic card, if able. Your team regains 1 spent " +
            "[Life] Life counter.";

        public const string SuggestionText =
            "If able, an enemy hero in radius moves 3 spaces in a straight line.";

        public const string DominatePersonText =
            "Target an enemy hero in radius. Defeat an enemy minion in radius adjacent to the target.";

        public const string PowerWordKillText = "Defeat an enemy hero in radius with no cards in hand.";

        public const string MidasTouchText =
            "Gain 1 coin for every two other spell cards removed from the spellbook.";

        public const string DisintegrateText =
            "Remove a token or an enemy non-heavy minion adjacent to you.";

        public const string PolymorphText =
            "Swap an enemy hero in radius with a token in radius or with an enemy minion in radius.";

        public const string ShieldText = "This round: You are immune to basic attacks.";

        public const string BanishmentText =
            "Place a unit or a token adjacent to you into a space in radius.";

        public const string InvulnerabilityText = "This Round: You are immune to non-basic attacks.";

        public const string FindFamiliarText =
            "Place a [Familiar] Familiar token in radius. You may remove up to three other spell cards " +
            "from the spell book faceup.";

        public const string DimensionDoorText =
            "Place yourself into a space at maximum radius. +1 [Radius] Radius for each other spell " +
            "card removed from the spellbook.";

        public const string CloudKillText = "An enemy hero in radius discards a basic card, if able.";

        public const string WishText =
            "Cast any spell in the Spellbook. After you cast the \"Wish\" spell three times your team " +
            "wins the game.";

        // ----------------------------------------------------------------------------------------
        // The table. One row per face: identity, printed name, colour, Tier (none = basic), the
        // printed primary and its words. A row whose text a later block composes carries
        // `Owed(kind, range, block, values)`; that block replaces the primary with its own builder.
        // ----------------------------------------------------------------------------------------

        private const string ReuseBlock = "9.30d";
        private const string ImmunityDiscardLifeMoveBlock = "9.30f";

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazySpells =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                // Gold, basic.
                Spell(MagicMissile, "Magic Missile", CardColor.Gold, CardTier.None, MagicMissileText,
                    MagicMissilePrimary()),
                Spell(ShockingGrasp, "Shocking Grasp", CardColor.Gold, CardTier.None, ShockingGraspText,
                    ShockingGraspPrimary()),
                Spell(ExpeditiousRetreat, "Expeditious Retreat", CardColor.Gold, CardTier.None,
                    ExpeditiousRetreatText,
                    ExpeditiousRetreatPrimary()),

                // Red (flame).
                Spell(BurningHands, "Burning Hands", CardColor.Red, CardTier.I, BurningHandsText,
                    BurningHandsPrimary()),
                Spell(Fireball, "Fireball", CardColor.Red, CardTier.II, FireballText,
                    FireballPrimary()),
                Spell(Sunburst, "Sunburst", CardColor.Red, CardTier.III, SunburstText,
                    SunburstPrimary()),

                // Red (skull).
                Spell(VampiricTouch, "Vampiric Touch", CardColor.Red, CardTier.II, VampiricTouchText,
                    VampiricTouchPrimary()),
                Spell(CreateUndead, "Create Undead", CardColor.Red, CardTier.II, CreateUndeadText,
                    CreateUndeadPrimary()),
                Spell(EnergyDrain, "Energy Drain", CardColor.Red, CardTier.III, EnergyDrainText,
                    EnergyDrainPrimary()),

                // Blue (eye).
                Spell(Suggestion, "Suggestion", CardColor.Blue, CardTier.I, SuggestionText,
                    SuggestionPrimary()),
                Spell(DominatePerson, "Dominate Person", CardColor.Blue, CardTier.II, DominatePersonText,
                    DominatePersonPrimary()),
                Spell(PowerWordKill, "Power Word Kill", CardColor.Blue, CardTier.III, PowerWordKillText,
                    PowerWordKillPrimary()),

                // Blue (ring).
                Spell(MidasTouch, "Midas' Touch", CardColor.Blue, CardTier.II, MidasTouchText,
                    MidasTouchPrimary()),
                Spell(Disintegrate, "Disintegrate", CardColor.Blue, CardTier.II, DisintegrateText,
                    DisintegratePrimary()),
                Spell(Polymorph, "Polymorph", CardColor.Blue, CardTier.III, PolymorphText,
                    PolymorphPrimary()),

                // Green (hand).
                Spell(Shield, "Shield", CardColor.Green, CardTier.I, ShieldText,
                    ShieldPrimary()),
                Spell(Banishment, "Banishment", CardColor.Green, CardTier.II, BanishmentText,
                    BanishmentPrimary()),
                Spell(Invulnerability, "Invulnerability", CardColor.Green, CardTier.III, InvulnerabilityText,
                    InvulnerabilityPrimary()),

                // Green (diamond).
                Spell(FindFamiliar, "Find Familiar", CardColor.Green, CardTier.II, FindFamiliarText,
                    FindFamiliarPrimary()),
                Spell(DimensionDoor, "Dimension Door", CardColor.Green, CardTier.II, DimensionDoorText,
                    DimensionDoorPrimary()),
                Spell(CloudKill, "Cloud Kill", CardColor.Green, CardTier.III, CloudKillText,
                    CloudKillPrimary()),

                // Purple (lamp), Tier IV.
                Spell(Wish, "Wish", CardColor.Purple, CardTier.IV, WishText,
                    WishPrimary())
            }));

        /// <summary>
        /// One spell face: no Initiative (a spell is cast, never played - 9.30b), its colour and Tier
        /// (no Tier = basic Gold), its one printed primary and its transcribed words.
        /// </summary>
        private static CardDefinition Spell(
            CardDefinitionId id, string name, CardColor color, CardTier tier, string text,
            ActionDefinition primary) =>
            new CardDefinition(
                id, ContentRevision, name, default,
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.SpellCard(color, tier),
                new[] { primary },
                CardSourceContent.Transcribed(text));
    }
}

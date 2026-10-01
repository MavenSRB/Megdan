using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// Milestone 9.30b. Test-only spell cards for the spellbook zone: one basic Gold spell, one Tier I
    /// Red spell and one Tier IV Purple spell, so every printed shape a spell takes (basic with no
    /// Tier; non-basic with a Tier; the Tier IV purple Skill) is held by the fixture. Fixture content,
    /// not a verified card or a rule-source interpretation; they print no action yet, because casting
    /// is a later milestone's.
    /// </summary>
    public static partial class SyntheticSpellbook
    {
        public const string ContentRevision = "9.30b-rev-1";

        public static readonly CardDefinitionId BasicSpellId = new CardDefinitionId("synthetic-spell-basic-gold");
        public static readonly CardDefinitionId TierOneSpellId = new CardDefinitionId("synthetic-spell-red-one");
        public static readonly CardDefinitionId TierFourSpellId = new CardDefinitionId("synthetic-spell-purple-four");

        /// <summary>The three spell definitions, in book order.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        /// <summary>
        /// The ordinary Red showcase hand with the three spells added to its hero's spellbook - the
        /// way a hero's content declares its spells: definitions beside the hand's, and one instance
        /// each created in the book.
        /// </summary>
        public static ConfiguredHand CreateRedWithSpellbook(HeroId ownerId)
        {
            var basis = SyntheticShowcaseHand.CreateRed(ownerId);
            return new ConfiguredHand(
                basis.Definitions.Concat(Definitions),
                basis.Instances,
                basis.DeckInstances,
                CreateSpellbookInstances(ownerId, "red"));
        }

        /// <summary>One instance of each spell, owned by <paramref name="ownerId"/>, in the spellbook.</summary>
        public static IReadOnlyList<CardInstance> CreateSpellbookInstances(HeroId ownerId, string instanceIdPrefix)
        {
            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException("Spell instance identities are per-seat.", nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(Definitions
                .Select(definition => CardInstance.CreateInSpellbook(
                    new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}"),
                    definition.Reference,
                    ownerId))
                .ToArray());
        }

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Spell(BasicSpellId, "TEST - Basic Spell", CardColor.Gold, CardTier.None),
                Spell(TierOneSpellId, "TEST - Tier I Spell", CardColor.Red, CardTier.I),
                Spell(TierFourSpellId, "TEST - Tier IV Spell", CardColor.Purple, CardTier.IV)
            }));

        // A spell prints no Initiative: it is never played, only cast from the book.
        private static CardDefinition Spell(CardDefinitionId id, string name, CardColor color, CardTier tier) =>
            new CardDefinition(
                id,
                ContentRevision,
                name,
                default,
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.SpellCard(color, tier));
    }
}

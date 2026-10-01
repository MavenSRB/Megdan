using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// One hero's cards as rulebook p.6's setup leaves them: the five they hold, the thirteen their
    /// deck keeps, and every definition either can name.
    ///
    /// <para>Milestone 8.4 separated this from the content that fills it. Until now the only hand this
    /// project had was <c>PlaceholderMovementHand</c>, which was both the carrier and the synthetic
    /// showcase cards, and the temporary-structure register named this milestone as the owner of its
    /// retirement. What retires is the standing-in: the configured session now deals
    /// <see cref="Verified.WaspCardContent"/>, and the showcase cards moved to
    /// <c>Synthetic/SyntheticShowcaseHand</c>, where their name says what they are. The carrier is not
    /// a placeholder and never was - a hand has this shape whoever printed the cards - so it stays,
    /// under a name that describes it.</para>
    ///
    /// <para>A hand supplies more definitions than it deals. p.15's Upgrade a Card searches the deck
    /// for a card one Tier higher, so the definitions those upgrades name have to be resolvable even
    /// before any hero owns one; <see cref="Definitions"/> is that whole set and
    /// <see cref="DealtDefinitions"/> is only what the hero starts holding.</para>
    /// </summary>
    public sealed class ConfiguredHand
    {
        public ConfiguredHand(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            IEnumerable<CardInstance> deckInstances)
            : this(definitions, instances, deckInstances, Array.Empty<CardInstance>())
        {
        }

        /// <summary>
        /// Milestone 9.30b. A hand whose hero also owns spell cards, created in the spellbook at setup
        /// (<see cref="CardInstance.CreateInSpellbook"/>). Their definitions belong in
        /// <paramref name="definitions"/> like every other card's.
        /// </summary>
        public ConfiguredHand(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            IEnumerable<CardInstance> deckInstances,
            IEnumerable<CardInstance> spellbookInstances)
        {
            Definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Instances = Array.AsReadOnly(
                (instances ?? throw new ArgumentNullException(nameof(instances))).ToArray());
            DeckInstances = Array.AsReadOnly(
                (deckInstances ?? throw new ArgumentNullException(nameof(deckInstances))).ToArray());
            SpellbookInstances = Array.AsReadOnly(
                (spellbookInstances ?? throw new ArgumentNullException(nameof(spellbookInstances))).ToArray());
            AllInstances = Array.AsReadOnly(
                Instances.Concat(DeckInstances).Concat(SpellbookInstances).ToArray());
            DealtDefinitions = Array.AsReadOnly(Instances
                .Select(instance => Definitions.First(definition =>
                    definition.Reference.Equals(instance.DefinitionReference)))
                .ToArray());
        }

        /// <summary>
        /// Every definition this hand supplies: the cards it deals, and since milestone 7.1 the
        /// upgrade targets those cards state. Pass this wherever a definition has to be resolvable -
        /// an offer query, a round authority, a snapshot - because a card can name content it has not
        /// been dealt.
        /// </summary>
        public IReadOnlyList<CardDefinition> Definitions { get; }

        /// <summary>
        /// The cards this hand deals into its owner's hand: rulebook p.6's five starting cards, plus
        /// whichever fixture Skill card a synthetic naming factory added.
        /// </summary>
        public IReadOnlyList<CardInstance> Instances { get; }

        /// <summary>
        /// Milestone 7.5. The cards this hand leaves in its owner's deck - p.6's "return the remaining
        /// Tier II, Tier III, and Tier IV cards to your deck" - which is every upgrade target the dealt
        /// cards lead to plus the Ultimate.
        ///
        /// They are instances rather than definitions alone because p.15's Upgrade a Card step looks
        /// for them there: "Find two Action cards in your deck that have the same color as the chosen
        /// card but are one Tier higher." A definition with no instance is content a hero could never
        /// take, so before that milestone the configured scene had a level nobody could complete.
        /// Together with <see cref="Instances"/> this makes the ordinary hand p.6's eighteen cards.
        ///
        /// They are a separate list rather than appended to <see cref="Instances"/> because the two say
        /// different things: one is what the hero holds at setup and the other is what waits in the
        /// deck, and <see cref="DealtDefinitions"/> - the set the approved card matrix is about - is
        /// exactly the first.
        /// </summary>
        public IReadOnlyList<CardInstance> DeckInstances { get; }

        /// <summary>
        /// Milestone 9.30b. The spell cards this hand's hero owns, which setup puts in the spellbook
        /// rather than dealing or leaving in the deck. Empty for every hero who owns no spell.
        /// </summary>
        public IReadOnlyList<CardInstance> SpellbookInstances { get; }

        /// <summary>Every card this hand supplies, hand, deck, then spellbook. What a match is composed with.</summary>
        public IReadOnlyList<CardInstance> AllInstances { get; }

        /// <summary>
        /// The definitions this hand actually deals, in the order it deals them, one per entry of
        /// <see cref="Instances"/>.
        ///
        /// This is the set the approved card matrix is about, and it is named separately from
        /// <see cref="Definitions"/> because milestone 7.1 made the two differ for the first time: a
        /// hand supplies the Tier II and Tier III cards its Tier I cards upgrade into, and none of them
        /// is dealt. A fixture asserting the five-card shape means this one.
        /// </summary>
        public IReadOnlyList<CardDefinition> DealtDefinitions { get; }

        public ConfiguredHandRestoreState CaptureRestoreState() =>
            new ConfiguredHandRestoreState(Definitions, Instances, DeckInstances, SpellbookInstances);

        public static ConfiguredHand Restore(ConfiguredHandRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var (definitions, instances, deckInstances, spellbookInstances) = state;
            var definitionArray = definitions.ToArray();
            var allInstances = instances.Concat(deckInstances).Concat(spellbookInstances).ToArray();
            if (definitionArray.Length == 0 || !instances.Any() ||
                allInstances.Select(instance => instance.OwnerId).Distinct().Count() != 1 ||
                allInstances.Any(instance => !definitionArray.Any(definition =>
                    definition.Reference.Equals(instance.DefinitionReference))))
            {
                throw new ArgumentException(
                    "A restored hand holds one owner's cards and every definition they reference.",
                    nameof(state));
            }

            return new ConfiguredHand(definitionArray, instances, deckInstances, spellbookInstances);
        }
    }
}

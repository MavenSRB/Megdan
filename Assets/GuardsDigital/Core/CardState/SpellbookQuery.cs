using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 9.30c. The three readings of a hero's spellbook that casting and refilling ask, stated
    /// once so the offer, the resolution and every other reader agree on them.
    ///
    /// <para><b>A spell is found by its definition, never by a card a chooser named.</b> Card text
    /// names a spell ("Cast the Burning Hands spell"), and every spell is one instance its owner holds
    /// (9.30b, GY-Q1), so the definition and the owner fix the instance. A decision about a spell
    /// therefore never has to carry a card identity from a facedown book to a seat that may not read
    /// it (GY-05: "choose the spells by name, without looking at them first").</para>
    ///
    /// <para><b>The two spellbook locations are never a hand, deck or discard</b>, and
    /// <see cref="IsInASpellbook"/> is how a reader that lists a hero's cards without naming a
    /// location says so (the 9.30c audit).</para>
    /// </summary>
    public static class SpellbookQuery
    {
        /// <summary>Whether a card lying in <paramref name="location"/> is a spell in the book or removed from it.</summary>
        public static bool IsInASpellbook(CardLocation location) =>
            location == CardLocation.Spellbook || location == CardLocation.RemovedFromSpellbook;

        /// <summary>
        /// <paramref name="ownerId"/>'s instance of the spell <paramref name="spellId"/> while it lies
        /// in the book, or null - the spell is removed, the hero owns no such spell, or no hero is
        /// named. One instance per definition (9.30b), so the lowest identity settles a malformed
        /// board deterministically rather than by collection order.
        /// </summary>
        public static CardInstance InBook(
            IEnumerable<CardInstance> instances, HeroId ownerId, CardDefinitionId spellId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!ownerId.IsDefined || !spellId.IsDefined)
            {
                return null;
            }

            return instances
                .Where(card => card != null &&
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.Spellbook &&
                    card.DefinitionReference.DefinitionId.Equals(spellId))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        /// <summary>
        /// Milestone 9.30e. "Each other spell card removed from the spellbook" - Sunburst, Dimension
        /// Door and Midas' Touch: how many spells the book of <paramref name="castingSpell"/>'s owner
        /// has removed, the casting spell itself excluded (lesson 27). The one derivation the value
        /// pipeline and the counting capability both ask, read when the value is asked: at an offer the
        /// casting spell still lies in the book, at the resolution it has just been removed, and the
        /// exclusion makes both moments count the same spells.
        ///
        /// <para>The book is the casting SPELL's owner's, not the caster's: the spellbook the text
        /// names is the one the spell came from (a non-owner caster, NebKher, GY-05). A card that is not
        /// in the given instances counts nothing.</para>
        /// </summary>
        public static int OtherRemoved(IEnumerable<CardInstance> instances, CardInstanceId castingSpell)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!castingSpell.IsDefined)
            {
                return 0;
            }

            var cards = instances as IReadOnlyCollection<CardInstance> ?? instances.ToArray();
            var casting = cards.FirstOrDefault(card => card != null && card.Id.Equals(castingSpell));
            return casting == null
                ? 0
                : Removed(cards, casting.OwnerId).Count(card => !card.Id.Equals(castingSpell));
        }

        /// <summary>Every spell <paramref name="ownerId"/> has removed from the book, in identity order.</summary>
        public static IReadOnlyList<CardInstance> Removed(
            IEnumerable<CardInstance> instances, HeroId ownerId)
        {
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (!ownerId.IsDefined)
            {
                return Array.Empty<CardInstance>();
            }

            return Array.AsReadOnly(instances
                .Where(card => card != null &&
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.RemovedFromSpellbook)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}

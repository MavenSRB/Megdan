using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 9.30h. What casting a counting spell from <c>caster</c>'s book would use right now,
    /// read off one seat's projection - the reading the spell choice's caption states (approved mockup
    /// A, "SUNBURST - 5 OUT: ATTACK 5, RANGE 5") so presentation formats numbers and never adds any.
    ///
    /// <para>Every input is table-public: the faceup removed pile (the count, lesson 27), the
    /// caster's projected item counts (p.16, as the rail's "6+1" badge reads them) and the spell's own
    /// definition. So the owner's seat, Hanu's delegated seat and the Director read the same values,
    /// and no facedown spell is consulted. The composition is <see cref="SpellCountReading"/>'s, over
    /// the rules' own <c>PrintedValueQuery</c> and <c>ItemBonusQuery</c>.</para>
    /// </summary>
    public static class SpellCastReading
    {
        /// <summary>
        /// The counting values of <paramref name="spell"/> cast now by <paramref name="caster"/>, or null
        /// for a spell whose text counts nothing.
        /// </summary>
        /// <param name="bonusPerItem">
        /// What one matching item adds right now - p.16's <see cref="ItemBonusQuery.BonusPerItem"/>, or
        /// the doubled amount a live source states (the reading the rail's badge is handed).
        /// </param>
        public static SpellCastValues Read(
            IMatchReadModel projection, CardDefinition spell, HeroId caster, int bonusPerItem)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (spell == null || !caster.IsDefined || !SpellCountReading.Counts(spell))
            {
                return null;
            }

            var others = SpellCountReading.OtherRemoved(projection.Cards, caster, spell.Id);
            var items = projection.HeroProgression
                .FirstOrDefault(value => value.HeroId.Equals(caster))?
                .ItemBonuses ?? Array.Empty<KeyValuePair<PrintedValueChannel, int>>();
            return new SpellCastValues(
                others,
                SpellCountReading.CastValues(spell, others, items, bonusPerItem),
                SpellCountReading.CountedCoins(spell, others));
        }
    }

    /// <summary>Milestone 9.30h. One counting spell's values for a cast now - see <see cref="SpellCastReading"/>.</summary>
    public sealed class SpellCastValues
    {
        public SpellCastValues(
            int otherRemoved,
            IReadOnlyList<KeyValuePair<PrintedValueChannel, int>> values,
            int? coins)
        {
            OtherRemoved = otherRemoved;
            Values = values ?? Array.Empty<KeyValuePair<PrintedValueChannel, int>>();
            Coins = coins;
        }

        /// <summary>The other spells removed from the book, the spell itself excluded.</summary>
        public int OtherRemoved { get; }

        /// <summary>Each counted channel's effective value - printed icon, the caster's items, the count.</summary>
        public IReadOnlyList<KeyValuePair<PrintedValueChannel, int>> Values { get; }

        /// <summary>The coins a counting step would gain (no item raises them), or null.</summary>
        public int? Coins { get; }
    }
}

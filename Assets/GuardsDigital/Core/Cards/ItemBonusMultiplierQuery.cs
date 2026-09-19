using System;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.61h. What one of a hero's rulebook p.16 items adds right now - the page's own +1,
    /// or the multiple a standing
    /// <see cref="RuleModificationKind.ItemBonusMultiplier"/> declaration states.
    ///
    /// <para><b>Why this is a query rather than a number anybody keeps.</b> Min the Dragonmonk's
    /// Inner Strength and Perfect Self print "This round: Double your item bonuses", which p.19 makes
    /// an Active effect that ends with the round. So the amount is a fact about the board at the
    /// moment a value is composed, true only while that effect stands and only for the hero it is
    /// declared about - which is exactly what <see cref="RuleModificationSet"/> already answers for
    /// every other sentence of that shape. Asking here means the same items add one point before her
    /// Skill resolves and two after it, with nothing about the items, the cards or the dashboard
    /// having changed.</para>
    ///
    /// <para><b>It is beside <see cref="ItemBonusQuery"/> rather than inside it, and the split is the
    /// same one p.16 already forced.</b> That type is a pure query over the printed result and the
    /// items it is handed; it decides the matching-icon condition and nothing else. This one reads the
    /// live rules set. Keeping them apart is what lets a caller that genuinely holds no set - a
    /// projection, a presentation reading, a composition with no rules at all - state the printed
    /// reading by naming <see cref="ItemBonusQuery.BonusPerItem"/> rather than by passing a fabricated
    /// empty set and hoping it means the same thing.</para>
    ///
    /// <para><b>Several declarations take the largest.</b> See
    /// <see cref="RuleModificationKind.ItemBonusMultiplier"/>: p.15 replaces the lower Tier, so the
    /// two faces that print this sentence can never sit in one deck, and p.10 allows one instance of
    /// an Active effect per card. A product would be a stacking rule no printed face asks for.</para>
    /// </summary>
    public static class ItemBonusMultiplierQuery
    {
        /// <summary>
        /// Rulebook p.16 unmodified: one item, one point. The multiplier every hero has and the one a
        /// caller holding no rules set states by name.
        /// </summary>
        public const int PrintedMultiplier = 1;

        /// <summary>
        /// The multiple of p.16's +1 that one of <paramref name="ownerId"/>'s items currently adds -
        /// <see cref="PrintedMultiplier"/> unless a declaration about that hero says otherwise.
        /// </summary>
        /// <param name="modifications">
        /// The live set. Required rather than optional by milestone 6.8f's rule: a caller that forgot
        /// it would compose an item bonus a source has already doubled.
        /// <see cref="RuleModificationSet.Empty"/> is the named none.
        /// </param>
        public static int For(HeroId ownerId, RuleModificationSet modifications)
        {
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (!ownerId.IsDefined)
            {
                return PrintedMultiplier;
            }

            var largest = PrintedMultiplier;
            var applied = modifications.For(RuleObject.ForHero(ownerId));
            for (var index = 0; index < applied.Count; index++)
            {
                var modification = applied[index].Modification;
                if (modification.Kind == RuleModificationKind.ItemBonusMultiplier &&
                    modification.Amount > largest)
                {
                    largest = modification.Amount;
                }
            }

            return largest;
        }

        /// <summary>
        /// What one matching item of <paramref name="ownerId"/>'s adds to the icon it raises, right
        /// now - p.16's <see cref="ItemBonusQuery.BonusPerItem"/> scaled by <see cref="For"/>. This is
        /// the number every composed total and the hero rail's badge read.
        /// </summary>
        public static int BonusPerItem(HeroId ownerId, RuleModificationSet modifications) =>
            checked(ItemBonusQuery.BonusPerItem * For(ownerId, modifications));

        /// <summary>
        /// The same multiple, read off the live Active effects instead of off a bound
        /// <see cref="RuleModificationSet"/>.
        ///
        /// <para><b>Two readings of one rule, and the second exists for the two callers that cannot
        /// ask the first.</b> <c>RuleModificationQuery</c> resolves an Active effect's declared Radius
        /// while it is BUILDING the set, so handing it one would be a circular question;
        /// <c>MinionDefeatBountyQuery</c> measures a standing promise's disk from a lifecycle and a
        /// ledger and is composed with no set at all. Both hold the effects themselves, which is one
        /// step earlier in the same derivation: the declaration read here is the identical
        /// <see cref="RuleModificationKind.ItemBonusMultiplier"/> the bound form would have found, and
        /// the only thing skipped is the binding those two callers are waiting on or were never
        /// given.</para>
        ///
        /// <para>Only <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> is read, because
        /// "your item bonuses" is a sentence about the hero whose card it is. A definition that put
        /// the family on somebody else would be a card doubling an opponent's items, which nothing
        /// prints; excluding it structurally keeps a later definition from widening this by
        /// accident.</para>
        /// </summary>
        public static int BonusPerItem(HeroId ownerId, ActiveEffectLifecycle timing) =>
            checked(ItemBonusQuery.BonusPerItem * For(ownerId, timing));

        /// <summary>
        /// The multiplier alone, read off the live Active effects - see
        /// <see cref="BonusPerItem(HeroId, ActiveEffectLifecycle)"/> for why this second reading
        /// exists.
        /// </summary>
        public static int For(HeroId ownerId, ActiveEffectLifecycle timing)
        {
            var largest = PrintedMultiplier;
            if (timing == null || !ownerId.IsDefined)
            {
                return largest;
            }

            foreach (var effect in timing.Effects)
            {
                if (effect == null ||
                    effect.Phase != ActiveEffectPhase.Active ||
                    effect.Definition.AffectedScope !=
                        ActiveEffectAffectedScopeKind.ControllerHero ||
                    !effect.ControllerHeroId.Equals(ownerId))
                {
                    continue;
                }

                var declared = effect.Definition.Modifications;
                for (var index = 0; index < declared.Count; index++)
                {
                    if (declared[index].Kind == RuleModificationKind.ItemBonusMultiplier &&
                        declared[index].Amount > largest)
                    {
                        largest = declared[index].Amount;
                    }
                }
            }

            return largest;
        }
    }
}

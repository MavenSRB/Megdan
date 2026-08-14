using System;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// What an object actually is, as represented on the table.
    ///
    /// This query deliberately takes no <see cref="RuleModificationSet"/>: rulebook p.18's Battle-Zone
    /// correction is "only relevant for minions represented by minion miniatures; objects that count as
    /// minions are unaffected", so a rule that cites representation must be unable to see a
    /// <c>Counts As</c> declaration even by accident. A capability chooses between this query and
    /// <see cref="EffectiveUnitKindQuery"/> explicitly, which is the choice
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> requires every capability to declare.
    /// </summary>
    public static class IntrinsicKindQuery
    {
        public static RuleObjectKind Of(RuleObject rulesObject)
        {
            if (!rulesObject.IsDefined)
            {
                throw new ArgumentException("An intrinsic-kind query requires a defined object.", nameof(rulesObject));
            }

            return rulesObject.Kind;
        }

        /// <summary>
        /// Whether this object is a minion miniature, which is the exact subject of the p.18 correction
        /// and of minion supply. A hero counting as a minion is never one.
        /// </summary>
        public static bool IsMinionMiniature(RuleObject rulesObject) =>
            Of(rulesObject) == RuleObjectKind.Minion;

        /// <summary>The intrinsic type expressed in the effective vocabulary, before any replacement.</summary>
        public static EffectiveUnitKind IntrinsicUnitKind(RuleObject rulesObject)
        {
            switch (Of(rulesObject))
            {
                case RuleObjectKind.Hero:
                    return EffectiveUnitKind.Hero;
                case RuleObjectKind.Minion:
                    return EffectiveUnitKind.Minion;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(rulesObject), rulesObject.Kind, "This object has no unit type.");
            }
        }
    }
}

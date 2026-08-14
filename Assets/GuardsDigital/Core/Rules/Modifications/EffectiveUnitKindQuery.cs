using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// What an object counts as for a type-based rule.
    ///
    /// Rulebook p.17 is a replacement, not an addition: "all rules and qualities of that object are
    /// replaced by the rules and qualities of the object type(s) it now counts as". A hero counting as a
    /// minion therefore stops satisfying a hero-typed query for as long as the declaration lasts, while
    /// <see cref="IntrinsicKindQuery"/> still reports the hero that is standing there - which is what
    /// keeps identity, ownership, occupancy, supply, and the p.18 miniature exception intact.
    /// </summary>
    public static class EffectiveUnitKindQuery
    {
        /// <summary>
        /// The object's effective types: the declared replacements when any apply, otherwise its
        /// intrinsic type.
        /// </summary>
        public static IReadOnlyList<EffectiveUnitKind> Of(
            RuleObject rulesObject,
            RuleModificationSet modifications)
        {
            if (!rulesObject.IsDefined)
            {
                throw new ArgumentException("An effective-type query requires a defined object.", nameof(rulesObject));
            }

            var declared = modifications?.DeclaredEffectiveUnitKinds(rulesObject);
            return declared != null && declared.Count > 0
                ? declared
                : new[] { IntrinsicKindQuery.IntrinsicUnitKind(rulesObject) };
        }

        public static bool Is(
            RuleObject rulesObject,
            EffectiveUnitKind effectiveUnitKind,
            RuleModificationSet modifications)
        {
            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "An effective-type query requires the type it asks about.", nameof(effectiveUnitKind));
            }

            var kinds = Of(rulesObject, modifications);
            for (var index = 0; index < kinds.Count; index++)
            {
                if (kinds[index] == effectiveUnitKind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

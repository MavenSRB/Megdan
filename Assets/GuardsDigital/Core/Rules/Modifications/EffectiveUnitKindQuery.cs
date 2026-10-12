using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

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
        /// intrinsic type - and then milestone 8.19c's overlays added to whichever of those two stands.
        ///
        /// The order is the only one the two families can compose in. p.17's replacement decides what
        /// the object counts as instead of what it is, so it must be resolved before anything is added;
        /// an overlay applied first would be swept away by the replacement it was meant to accompany.
        /// Xargatha's Stone Gaze is the shape that needs the addition - "count as both heroes and
        /// terrain" - and it stays true whether or not something else has already replaced the hero's
        /// type, because "both" names the reading that stands, not the miniature.
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
            // A token is intrinsically no unit, yet a card may make it count as one for a bounded
            // action. Keep its ordinary type empty and add only the explicitly live overlay.
            var standing = declared != null && declared.Count > 0
                ? declared
                : rulesObject.Kind == RuleObjectKind.Token
                    ? (IReadOnlyList<EffectiveUnitKind>)Array.Empty<EffectiveUnitKind>()
                    : new[] { IntrinsicKindQuery.IntrinsicUnitKind(rulesObject) };
            var overlaid = modifications?.OverlaidEffectiveUnitKinds(rulesObject);
            var modeMinion = rulesObject.Kind == RuleObjectKind.Hero &&
                modifications?.ModeRules?.HasMinionCategory(rulesObject.HeroId) == true;
            if ((overlaid == null || overlaid.Count == 0) && !modeMinion)
            {
                return standing;
            }

            var composed = new List<EffectiveUnitKind>(standing);
            if (modeMinion && !composed.Contains(EffectiveUnitKind.Minion))
                composed.Add(EffectiveUnitKind.Minion);
            for (var index = 0; index < (overlaid?.Count ?? 0); index++)
            {
                if (!composed.Contains(overlaid[index]))
                {
                    composed.Add(overlaid[index]);
                }
            }

            return composed.AsReadOnly();
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

        /// <summary>
        /// The standard class explicitly declared for a token made to count as a minion.
        /// An untyped Minion overlay has no p.14 melee/ranged/heavy modifier.
        /// </summary>
        public static StandardMinionType TokenMinionType(
            RuleObject token, RuleModificationSet modifications)
        {
            if (token.Kind != RuleObjectKind.Token)
                throw new ArgumentException("The effective token class requires a token.", nameof(token));
            var declared = modifications?.Modifications
                .Where(applied =>
                    applied.AffectedObject.Equals(token) &&
                    applied.Modification.Kind == RuleModificationKind.EffectiveTypeOverlay &&
                    applied.Modification.EffectiveUnitKind == EffectiveUnitKind.Minion &&
                    Enum.IsDefined(typeof(StandardMinionType),
                        applied.Modification.Amount) &&
                    applied.Modification.Amount != (int)StandardMinionType.None)
                .Select(applied => (StandardMinionType)applied.Modification.Amount)
                .ToArray();
            return declared == null || declared.Length == 0
                ? StandardMinionType.None
                : declared[declared.Length - 1];
        }

        /// <summary>
        /// A token made friendly by a counts-as effect belongs to that effect's controller for
        /// action-scoped minion queries, not necessarily to the hero who originally placed it.
        /// </summary>
        public static HeroId TokenMinionController(
            RuleObject token, RuleModificationSet modifications)
        {
            if (token.Kind != RuleObjectKind.Token)
                throw new ArgumentException("The effective token controller requires a token.", nameof(token));
            return modifications?.Modifications
                .Where(applied =>
                    applied.AffectedObject.Equals(token) &&
                    applied.Modification.Kind == RuleModificationKind.EffectiveTypeOverlay &&
                    applied.Modification.EffectiveUnitKind == EffectiveUnitKind.Minion)
                .Select(applied => applied.ControllerHeroId)
                .LastOrDefault() ?? default;
        }
    }
}

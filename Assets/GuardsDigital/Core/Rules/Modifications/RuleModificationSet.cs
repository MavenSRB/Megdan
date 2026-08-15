using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Every modification in effect at one authoritative moment, in the order the rules apply them.
    ///
    /// It is a value: built from live state by <see cref="RuleModificationQuery"/>, never mutated, and
    /// safe to hand to a pure query. A query that consults this set is explicit about doing so, which is
    /// what keeps "which rules currently apply" out of the individual algorithms.
    ///
    /// Ordering follows rulebook p.17: simultaneous Active effects are ordered "using the same method as
    /// when resolving tied initiative (See page 7), but do not flip the Tie Breaker coin". The revealed
    /// side therefore orders first, the source's creation sequence second, and its stable identity last so
    /// the order is total rather than merely mostly determined.
    /// </summary>
    public sealed class RuleModificationSet
    {
        /// <summary>
        /// The named no-modification value. Nothing is in effect, so nothing needs ordering and the
        /// Tie Breaker is genuinely unrevealed rather than merely unsupplied.
        /// </summary>
        public static RuleModificationSet Empty { get; } =
            new RuleModificationSet(Array.Empty<AppliedRuleModification>(), TieBreakerState.Unrevealed);

        /// <summary>
        /// The Tie Breaker is required since milestone 6.8i, because it decides the order this set
        /// resolves in; a caller with genuinely no ordering state passes
        /// <see cref="TieBreakerState.Unrevealed"/>.
        /// </summary>
        public RuleModificationSet(
            IEnumerable<AppliedRuleModification> modifications,
            TieBreakerState tieBreaker)
        {
            var materialized = (modifications ?? throw new ArgumentNullException(nameof(modifications)))
                .ToArray();
            if (materialized.Any(modification => modification == null))
            {
                throw new ArgumentException("Applied modifications must be non-null.", nameof(modifications));
            }

            TieBreaker = tieBreaker;
            Modifications = new ReadOnlyCollection<AppliedRuleModification>(materialized
                .OrderBy(modification => tieBreaker.IsDefined &&
                    modification.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(modification => modification.OrderingSequence)
                .ThenBy(modification => modification.SourceId, StringComparer.Ordinal)
                .ThenBy(modification => modification.Modification.Kind)
                .ToArray());
        }

        /// <summary>The Tie-Breaker state this order was resolved against, when one was supplied.</summary>
        public TieBreakerState TieBreaker { get; }

        public IReadOnlyList<AppliedRuleModification> Modifications { get; }

        public bool IsEmpty => Modifications.Count == 0;

        /// <summary>Everything affecting one object, in canonical order.</summary>
        public IReadOnlyList<AppliedRuleModification> For(RuleObject affectedObject) =>
            Array.AsReadOnly(Modifications
                .Where(modification => modification.AffectedObject.Equals(affectedObject))
                .ToArray());

        /// <summary>
        /// Whether <paramref name="affectedObject"/> is currently barred from <paramref name="activity"/>.
        /// A permission never answers this question: p.18's one general permission allows a unit to enter
        /// an obstacle's space, which is a grant against a general rule, not a licence that cancels an
        /// unrelated prohibition. Keeping the two families in separate queries is what makes a narrow
        /// permission structurally unable to disable a restriction it does not name.
        /// </summary>
        public bool IsRestricted(RuleObject affectedObject, RuleActivity activity) =>
            TryGetRestriction(affectedObject, activity, out _);

        /// <summary>
        /// The first restriction in canonical order that bars the activity, which is the one whose source
        /// explains the refusal.
        /// </summary>
        public bool TryGetRestriction(
            RuleObject affectedObject,
            RuleActivity activity,
            out AppliedRuleModification restriction)
        {
            restriction = FirstCovering(affectedObject, RuleModificationKind.Restriction, activity);
            return restriction != null;
        }

        public bool HasPermission(RuleObject affectedObject, RulePermissionKind permission) =>
            TryGetPermission(affectedObject, permission, out _);

        public bool TryGetPermission(
            RuleObject affectedObject,
            RulePermissionKind permission,
            out AppliedRuleModification granted)
        {
            if (permission == RulePermissionKind.None)
            {
                throw new ArgumentException("A permission query requires the exact grant.", nameof(permission));
            }

            granted = Modifications.FirstOrDefault(modification =>
                modification.AffectedObject.Equals(affectedObject) &&
                modification.Modification.Kind == RuleModificationKind.Permission &&
                modification.Modification.Permission == permission);
            return granted != null;
        }

        /// <summary>
        /// Whether <paramref name="target"/> is immune to <paramref name="actor"/>'s
        /// <paramref name="activity"/>.
        ///
        /// Rulebook p.18 closes with "You are never immune to your own actions", so a defined actor that
        /// is the target itself is answered before any declaration is consulted. An undefined actor asks
        /// the question in the abstract - no actor is performing anything - and that exception does not
        /// apply.
        ///
        /// The actor is required since milestone 6.8i. Which of those two questions is being asked is
        /// exactly what an omitted actor decided in silence, and it decided it the wrong way for every
        /// caller that had one to name: the p.18 exception simply stopped applying. A caller genuinely
        /// asking in the abstract writes <see cref="RuleObject.None"/>.
        /// </summary>
        public bool IsImmuneTo(RuleObject target, RuleActivity activity, RuleObject actor) =>
            TryGetImmunity(target, activity, actor, out _);

        public bool TryGetImmunity(
            RuleObject target,
            RuleActivity activity,
            RuleObject actor,
            out AppliedRuleModification immunity)
        {
            immunity = null;
            if (actor.IsDefined && actor.Equals(target))
            {
                return false;
            }

            immunity = FirstCovering(target, RuleModificationKind.Immunity, activity);
            return immunity != null;
        }

        /// <summary>
        /// Whether the object effectively counts as <paramref name="effectiveUnitKind"/>. Rulebook p.17
        /// replaces the object's rules and qualities with the declared type's, so this answers only the
        /// effective question; <see cref="IntrinsicKindQuery"/> answers what the object still is.
        /// </summary>
        public bool CountsAs(RuleObject affectedObject, EffectiveUnitKind effectiveUnitKind)
        {
            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "A Counts As query requires the effective type.", nameof(effectiveUnitKind));
            }

            return Modifications.Any(modification =>
                modification.AffectedObject.Equals(affectedObject) &&
                modification.Modification.Kind == RuleModificationKind.CountsAs &&
                modification.Modification.EffectiveUnitKind == effectiveUnitKind);
        }

        /// <summary>
        /// Every declared effective type for one object, in canonical order and without duplicates.
        /// Empty when nothing replaces the object's intrinsic type.
        /// </summary>
        public IReadOnlyList<EffectiveUnitKind> DeclaredEffectiveUnitKinds(RuleObject affectedObject) =>
            Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.CountsAs)
                .Select(modification => modification.Modification.EffectiveUnitKind)
                .Distinct()
                .ToArray());

        /// <summary>
        /// The typed contributions this set adds to one value channel for one object, in canonical order.
        /// Each keeps its own source discriminator, so they compose with the printed, minion, item, and
        /// card-text contributions rather than replacing them.
        /// </summary>
        public IReadOnlyList<ValueContribution> ValueContributions(
            RuleObject affectedObject,
            PrintedValueChannel channel)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A value query requires a channel.", nameof(channel));
            }

            return Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.ValueModifier &&
                    modification.Modification.Channel == channel)
                .Select(modification => modification.ToValueContribution())
                .ToArray());
        }

        private AppliedRuleModification FirstCovering(
            RuleObject affectedObject,
            RuleModificationKind kind,
            RuleActivity activity)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("This query requires a named activity.", nameof(activity));
            }

            return Modifications.FirstOrDefault(modification =>
                modification.AffectedObject.Equals(affectedObject) &&
                modification.Modification.Kind == kind &&
                modification.Modification.Covers(activity));
        }
    }
}

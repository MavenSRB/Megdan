using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Board;
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
    ///
    /// Milestone 8.7 adds a second collection beside the modifications: the conditional obstacle
    /// overlays in effect, which change p.18's classification of spaces rather than anything about an
    /// object. They ride here because this value is what every rules seam already means by "the rules
    /// right now", and because carrying them separately would have given the obstacle question two
    /// sources to consult and one of them to forget.
    /// </summary>
    public sealed class RuleModificationSet
    {
        /// <summary>
        /// The named no-modification value. Nothing is in effect, so nothing needs ordering and the
        /// Tie Breaker is genuinely unrevealed rather than merely unsupplied.
        /// </summary>
        public static RuleModificationSet Empty { get; } =
            new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(),
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// The Tie Breaker is required since milestone 6.8i, because it decides the order this set
        /// resolves in; a caller with genuinely no ordering state passes
        /// <see cref="TieBreakerState.Unrevealed"/>.
        ///
        /// Milestone 8.7's obstacle overlays are required for the same reason and are stated the same
        /// way: a caller holding none passes an empty sequence, and one that forgot them would have
        /// produced a board whose spaces quietly stopped being obstacles.
        /// </summary>
        public RuleModificationSet(
            IEnumerable<AppliedRuleModification> modifications,
            IEnumerable<AppliedObstacleOverlay> obstacleOverlays,
            TieBreakerState tieBreaker)
        {
            var materialized = (modifications ?? throw new ArgumentNullException(nameof(modifications)))
                .ToArray();
            if (materialized.Any(modification => modification == null))
            {
                throw new ArgumentException("Applied modifications must be non-null.", nameof(modifications));
            }

            var materializedOverlays =
                (obstacleOverlays ?? throw new ArgumentNullException(nameof(obstacleOverlays)))
                .ToArray();
            if (materializedOverlays.Any(overlay => overlay == null))
            {
                throw new ArgumentException(
                    "Applied obstacle overlays must be non-null.", nameof(obstacleOverlays));
            }

            ObstacleOverlays = new ReadOnlyCollection<AppliedObstacleOverlay>(materializedOverlays
                .OrderBy(overlay => tieBreaker.IsDefined &&
                    overlay.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(overlay => overlay.SourceId, StringComparer.Ordinal)
                .ToArray());
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

        /// <summary>
        /// Milestone 8.7's conditional obstacle overlays, already bound to this moment's anchor and
        /// acting hero. They are carried here rather than threaded separately because this value is
        /// already "the rules in effect right now" and already reaches every seam that asks p.18's
        /// obstacle question.
        /// </summary>
        public IReadOnlyList<AppliedObstacleOverlay> ObstacleOverlays { get; }

        public bool IsEmpty => Modifications.Count == 0 && ObstacleOverlays.Count == 0;

        /// <summary>
        /// Whether any overlay in effect makes <paramref name="space"/> count as an obstacle right
        /// now. <see cref="ObstacleQuery"/> is the consumer; nothing else may ask, because p.18 gives
        /// the classification one owner.
        /// </summary>
        public bool AnyOverlayMakesAnObstacleOf(HexCoordinate space)
        {
            for (var index = 0; index < ObstacleOverlays.Count; index++)
            {
                if (ObstacleOverlays[index].CountsAsObstacle(space))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Everything affecting one object, in canonical order.</summary>
        public IReadOnlyList<AppliedRuleModification> For(RuleObject affectedObject) =>
            Array.AsReadOnly(Modifications
                .Where(modification => modification.AffectedObject.Equals(affectedObject))
                .ToArray());

        /// <summary>
        /// Whether <paramref name="affectedObject"/> is currently barred from performing
        /// <paramref name="activity"/> itself.
        ///
        /// A permission never answers this question: p.18's one general permission allows a unit to enter
        /// an obstacle's space, which is a grant against a general rule, not a licence that cancels an
        /// unrelated prohibition. Keeping the two families in separate queries is what makes a narrow
        /// permission structurally unable to disable a restriction it does not name.
        ///
        /// Since milestone 8.7 a restriction may name who is barred, and this asks the one question an
        /// offer query has: the actor and the restricted object are the same hero, so a declaration
        /// that names only other people's heroes leaves the offer standing.
        /// <see cref="RestrictionQuery"/> answers the directed form, where they differ.
        /// </summary>
        public bool IsRestricted(RuleObject affectedObject, RuleActivity activity) =>
            TryGetRestriction(affectedObject, activity, out _);

        /// <summary>
        /// The first restriction in canonical order that bars the object from performing the activity,
        /// which is the one whose source explains the refusal.
        /// </summary>
        public bool TryGetRestriction(
            RuleObject affectedObject,
            RuleActivity activity,
            out AppliedRuleModification restriction)
        {
            restriction = CoveringRestrictions(affectedObject, activity)
                .FirstOrDefault(applied => applied.Modification.RestrictedActors.CoversTheAffectedObject);
            return restriction != null;
        }

        /// <summary>
        /// Every restriction on this object that reaches this activity, in canonical order, whatever
        /// actors each names. <see cref="RestrictionQuery"/> is the consumer: it holds the participants
        /// a declared team relation is resolved against, which this value deliberately does not.
        /// </summary>
        public IReadOnlyList<AppliedRuleModification> CoveringRestrictions(
            RuleObject affectedObject, RuleActivity activity)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("This query requires a named activity.", nameof(activity));
            }

            return Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.Restriction &&
                    modification.Modification.Covers(activity))
                .ToArray());
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

        /// <summary>
        /// The first immunity in canonical order that this set can decide on its own.
        ///
        /// Since milestone 8.19c an immunity may name the actors it protects against, and this value
        /// holds no participants to resolve a team relation with - the same division restrictions have
        /// had since milestone 8.7. So this answers for the declarations that need nobody resolved: an
        /// unqualified one, and one naming the affected object itself when the actor is that object -
        /// which p.18 then refuses anyway, above. <see cref="ImmunityQuery"/> is the asker that holds
        /// the participants and therefore the one that can answer a qualified declaration; a caller
        /// with a directed question must go through it or silently widen "by enemy heroes" into "by
        /// anyone".
        /// </summary>
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

            var covering = CoveringImmunities(target, activity);
            for (var index = 0; index < covering.Count; index++)
            {
                if (covering[index].Modification.RestrictedActors.IsUnqualified)
                {
                    immunity = covering[index];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Every immunity on this object that reaches this activity, in canonical order, whatever
        /// actors each names. <see cref="ImmunityQuery"/> is the consumer, for the reason
        /// <see cref="RestrictionQuery"/> is the consumer of <see cref="CoveringRestrictions"/>: it
        /// holds the participants a declared team relation is resolved against.
        /// </summary>
        public IReadOnlyList<AppliedRuleModification> CoveringImmunities(
            RuleObject affectedObject, RuleActivity activity)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("This query requires a named activity.", nameof(activity));
            }

            return Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.Immunity &&
                    modification.Modification.Covers(activity))
                .ToArray());
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
        /// Milestone 8.19c. Every type one object counts as <em>in addition to</em> what it already
        /// counts as, in canonical order and without duplicates. Empty when nothing overlays it.
        ///
        /// Kept apart from <see cref="DeclaredEffectiveUnitKinds"/> because the two compose in one
        /// direction only: a p.17 replacement decides what the intrinsic type becomes, and an overlay
        /// then adds to whatever that answer was. <see cref="EffectiveUnitKindQuery"/> is where the two
        /// meet, so no consumer has to know the order.
        /// </summary>
        public IReadOnlyList<EffectiveUnitKind> OverlaidEffectiveUnitKinds(RuleObject affectedObject) =>
            Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.EffectiveTypeOverlay)
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
            PrintedValueChannel channel) =>
            ValueContributions(affectedObject, channel, RuleValueApplicability.Always);

        /// <summary>
        /// Milestone 8.19c. The same contributions for a consumer that knows which action is being
        /// performed, so `modify-value@2`'s qualified declarations can reach it.
        ///
        /// <paramref name="performedAction"/> is what the caller is composing a total <em>for</em>, and
        /// it admits both the declarations qualified to it and the unqualified ones - a source that
        /// says "+1 Defense" applies to the Defense being calculated whatever else is true. A caller
        /// holding no action passes <see cref="RuleValueApplicability.Always"/> and sees only the
        /// unqualified ones, which is the honest answer: a qualification nobody can evaluate has not
        /// been met.
        /// </summary>
        public IReadOnlyList<ValueContribution> ValueContributions(
            RuleObject affectedObject,
            PrintedValueChannel channel,
            RuleValueApplicability performedAction)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A value query requires a channel.", nameof(channel));
            }

            if (!Enum.IsDefined(typeof(RuleValueApplicability), performedAction))
            {
                throw new ArgumentException(
                    "A value query states the action class it is composing a total for.",
                    nameof(performedAction));
            }

            return Array.AsReadOnly(Modifications
                .Where(modification =>
                    modification.AffectedObject.Equals(affectedObject) &&
                    modification.Modification.Kind == RuleModificationKind.ValueModifier &&
                    modification.Modification.Channel == channel &&
                    (modification.Modification.ValueApplicability == RuleValueApplicability.Always ||
                        modification.Modification.ValueApplicability == performedAction))
                .Select(modification => modification.ToValueContribution())
                .ToArray());
        }
    }
}

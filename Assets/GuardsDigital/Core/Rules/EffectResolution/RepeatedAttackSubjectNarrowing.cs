using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 8.75d. What the clause that produced a REPEATED Attack instance lets that instance
    /// take - the unit kind it admits, and the units it has already taken - stated in the vocabulary
    /// a target SELECTION can read.
    ///
    /// <para><b>Why the fact has to travel at all.</b> Rulebook p.13's own target question already
    /// applies both halves: <c>AttackDefenseCardActionStrategyBackend.FindLegalAttackTargets</c>'
    /// caller narrows a repeated instance to the kind
    /// <see cref="GuardsDigital.Core.Effects.RepeatTargetBinding"/> names and removes what the asking
    /// instance took. An Attack whose target is bound by CARD TEXT
    /// (<see cref="GuardsDigital.Core.Cards.AttackTargetBinding.ByCardText"/>) never asks that
    /// question: it reaches p.14's step 1 with nothing bound and its own printed subject names a unit
    /// instead. So the repeated instance re-ran the printed sentence whole, and the sentence has never
    /// heard of the clause that produced the instance - the offer admitted the unit the first instance
    /// attacked, and for a clause naming a KIND it admitted the wrong kind outright. That is the
    /// offer-admits-more-than-the-answer-can-perform class of milestones 8.29d, 8.29e, 8.35z5 and
    /// 8.35z15, recorded as ID-Q10c by milestone 8.75c.</para>
    ///
    /// <para><b>It is derived, never stored</b>, exactly as
    /// <see cref="GuardsDigital.Core.Effects.RepeatTargetBinding.DifferentEnemyHero"/>'s own remarks
    /// record: the repeated instance reads its clause off the trusted definition and its exclusions
    /// off the resolution it is already carrying, both at the moment the enclosing facts are built. So
    /// this moves no snapshot field and no schema version, and a tampered payload cannot widen the set
    /// back.</para>
    ///
    /// <para><b>It speaks p.19's board vocabulary rather than p.13's.</b> An attack target is an
    /// <c>AttackTarget</c>, but the thing that has to be narrowed is a selection's candidate list, and
    /// a selection names heroes, minions and spaces. Carrying
    /// <see cref="BoardObjectReference"/> is the same choice
    /// <see cref="GuardsDigital.Core.Effects.RepeatContinuation.BoundTarget"/> already made for
    /// "on the same target", and for the same reason: the piece is what the clause names, and a whole
    /// action boundary stands between the two instances.</para>
    /// </summary>
    public readonly struct RepeatedAttackSubjectNarrowing
        : IEquatable<RepeatedAttackSubjectNarrowing>
    {
        private readonly IReadOnlyList<BoardObjectReference> excludedUnits;

        public RepeatedAttackSubjectNarrowing(
            BoardObjectKind admittedKind,
            IEnumerable<BoardObjectReference> excludedUnits)
        {
            if (!Enum.IsDefined(typeof(BoardObjectKind), admittedKind) ||
                admittedKind == BoardObjectKind.Token)
            {
                throw new ArgumentException(
                    "Rulebook p.13 lets an Attack take a hero or a minion, so a repeat clause " +
                    "admits one of those kinds or states no narrowing at all.",
                    nameof(admittedKind));
            }

            AdmittedKind = admittedKind;
            this.excludedUnits = new ReadOnlyCollection<BoardObjectReference>(
                (excludedUnits ?? Enumerable.Empty<BoardObjectReference>())
                    .Where(unit => unit.IsDefined)
                    .Distinct()
                    .ToArray());
        }

        /// <summary>
        /// No narrowing: every printed instance, and every repeated one whose clause says nothing
        /// about what it may take. It is the default value, so a frame outside an Attack and every
        /// caller built before this milestone states it by stating nothing.
        /// </summary>
        public static RepeatedAttackSubjectNarrowing None => default;

        /// <summary>
        /// The one unit kind the producing clause admits - <c>Hero</c> for "on a different hero",
        /// <c>Minion</c> for "on a minion" - or <see cref="BoardObjectKind.None"/> where the clause
        /// leaves p.19's kinds exactly as the printed instance had them.
        /// </summary>
        public BoardObjectKind AdmittedKind { get; }

        /// <summary>
        /// The units this repeat may not take again, which is what "a DIFFERENT hero" and rulebook
        /// p.10's "you cannot target the same unit" each name. Empty for a clause that excludes
        /// nobody, and never null.
        /// </summary>
        public IReadOnlyList<BoardObjectReference> ExcludedUnits =>
            excludedUnits ?? Array.Empty<BoardObjectReference>();

        /// <summary>Whether this narrows anything at all.</summary>
        public bool IsDefined =>
            AdmittedKind != BoardObjectKind.None || ExcludedUnits.Count > 0;

        /// <summary>Whether the clause that produced this instance lets it take this piece.</summary>
        public bool Admits(BoardObjectReference unit) =>
            (AdmittedKind == BoardObjectKind.None || unit.Kind == AdmittedKind) &&
            !ExcludedUnits.Contains(unit);

        /// <summary>
        /// Whether a piece of this kind is admitted at all, asked where the candidate is a SPACE and
        /// the kind is all that can be read of it.
        /// </summary>
        public bool AdmitsKind(BoardObjectKind kind) =>
            AdmittedKind == BoardObjectKind.None || kind == AdmittedKind;

        public bool Equals(RepeatedAttackSubjectNarrowing other) =>
            AdmittedKind == other.AdmittedKind &&
            ExcludedUnits.Count == other.ExcludedUnits.Count &&
            ExcludedUnits.SequenceEqual(other.ExcludedUnits);

        public override bool Equals(object obj) =>
            obj is RepeatedAttackSubjectNarrowing other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)AdmittedKind * 397;
                foreach (var unit in ExcludedUnits)
                {
                    hash = (hash * 397) ^ unit.GetHashCode();
                }

                return hash;
            }
        }

        public override string ToString() =>
            !IsDefined
                ? "unnarrowed"
                : $"{(AdmittedKind == BoardObjectKind.None ? "any unit" : AdmittedKind.ToString())}" +
                  $" except {ExcludedUnits.Count}";
    }
}

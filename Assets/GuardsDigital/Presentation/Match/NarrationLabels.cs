using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The one place the narration lane's vocabulary is composed. Milestone 8.35n (the FIFTH
    /// amendment, playtest finding: "ACTING ON INITIATIVE X is not enough ... hero name, who is
    /// acting, not only initiative"). It exists so the fizzle notice (SIXTH amendment) and the
    /// player-facing log (TENTH amendment) reuse exactly these strings rather than each inventing its
    /// own hero-plus-activity phrasing, per the batch handoff's own vocabulary-convention note.
    ///
    /// <para>Every string here is one of two shapes, matching the derivability split
    /// <c>RoundActionPresenter.FormatStatus</c> builds from: a decision the VIEWER'S OWN projection
    /// carries in full (<see cref="Own"/>), or a fact public to every viewer regardless of ownership
    /// (<see cref="HeroActing"/>, <see cref="HeroAwaitingRelease"/>, <see cref="WaitingOn"/>). Nothing
    /// here decides which shape applies - that is the caller's own read of the projection, exactly as
    /// <see cref="AwaitedInputQuery"/> keeps the "waiting on" predicate itself out of the surfaces that
    /// consume it.</para>
    /// </summary>
    public static class NarrationLabels
    {
        /// <summary>
        /// A decision the VIEWER'S OWN projection carries in full - defense, leveling, respawn,
        /// movement, an attack target, and the rest of the eleven declared pending-decision families,
        /// plus the viewer's own acting turn and the viewer's own release. Addressed to the viewer
        /// directly, because a seat projection carries these fields only for the seat that owns the
        /// standing decision (the Director excepted, who owns every family at once).
        /// </summary>
        public static string Own(string family, string activity)
        {
            if (string.IsNullOrWhiteSpace(family))
            {
                throw new ArgumentException("An own-decision line names its family.", nameof(family));
            }

            if (string.IsNullOrWhiteSpace(activity))
            {
                throw new ArgumentException("An own-decision line names its activity.", nameof(activity));
            }

            return $"YOUR {family} — {activity}";
        }

        /// <summary>
        /// The acting hero's name, for every viewer. Built from
        /// <see cref="IMatchReadModel.SelectedActor"/> alone, which is public and unscoped - the same
        /// fact the acting-hero board highlight reads. It says only that this hero is currently
        /// acting, never whether a decision (a defender's answer, say) is standing on top of that
        /// turn: nothing on <see cref="IMatchReadModel"/> tells a non-owning viewer that today, and the
        /// gap is a recorded non-goal - the same one <see cref="AwaitedInputQuery.IsHeroAwaited"/>
        /// already records for the hero-scope tray shine - rather than an oversight here.
        /// </summary>
        public static string HeroActing(string heroDisplayName, int initiative) =>
            $"{RequireName(heroDisplayName)} — ACTING (INITIATIVE {initiative})";

        /// <summary>
        /// Milestone 8.35g's release, named for every OTHER viewer by the hero it is standing on -
        /// <c>PendingTurnRelease.ActorId</c> is unscoped by that milestone's own deliberate decision,
        /// so every seat's projection carries it regardless of ownership.
        /// </summary>
        public static string HeroAwaitingRelease(string heroDisplayName) =>
            $"{RequireName(heroDisplayName)} — TURN COMPLETE, AWAITING RELEASE";

        /// <summary>
        /// The heroes still owed a public answer - a commitment, or an unanswered team-agreement
        /// participant. Every entry is a fact <see cref="IMatchReadModel"/> already publishes to
        /// every seat (<c>Commitments</c> and <c>TeamAgreement.Participants</c> are both stated public
        /// on their own declarations), so naming them widens no privacy boundary.
        /// </summary>
        public static string WaitingOn(IEnumerable<string> heroDisplayNames)
        {
            var names = (heroDisplayNames ?? Enumerable.Empty<string>()).ToArray();
            if (names.Length == 0)
            {
                throw new ArgumentException(
                    "A waiting-on line requires at least one named hero.", nameof(heroDisplayNames));
            }

            return "WAITING ON: " + string.Join(", ", names);
        }

        /// <summary>
        /// Milestone 8.35x. The honest alternative to a <see cref="WaitingOn"/> line once every hero
        /// it would have named has already answered - the legal transient state between the last
        /// answer landing and the authority resolving what it unblocked.
        ///
        /// <para>It exists because <see cref="WaitingOn"/> is deliberately, and correctly, defensive:
        /// a waiting-on line naming nobody is a meaningless sentence, so the constructor refuses to
        /// build one. That leaves the CALLER owing a real sentence for the empty set, and until this
        /// milestone the two team-agreement branches of <c>RoundActionPresenter.FormatStatus</c> owed
        /// it and did not pay - they handed the empty set straight to <see cref="WaitingOn"/>, whose
        /// throw took the whole round panel down on every refresh for the rest of the match. The
        /// vocabulary's own guard is kept exactly as it was; what changed is that the callers now
        /// answer the empty case themselves instead of asking for a line that cannot exist.</para>
        ///
        /// <para>It claims only what an empty owed-set actually proves - that no participant is still
        /// owed - and never that the match has moved on, which is a fact no projection publishes
        /// here.</para>
        /// </summary>
        public const string EveryAnswerIn = "EVERY ANSWER IN";

        private static string RequireName(string heroDisplayName) =>
            string.IsNullOrWhiteSpace(heroDisplayName)
                ? throw new ArgumentException(
                    "A public narration line requires the projected hero display name.",
                    nameof(heroDisplayName))
                : heroDisplayName;
    }
}

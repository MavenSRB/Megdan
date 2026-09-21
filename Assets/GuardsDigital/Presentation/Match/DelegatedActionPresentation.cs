using GuardsDigital.Application;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.72. <b>Which card's controls does this seat draw, and why.</b> One owner, because
    /// until this milestone the question was answered in three unrelated places and two of them were
    /// answering a different question.
    ///
    /// <para><b>The gap this closes, in the user's words: "Q15i means that currently that card wont
    /// work".</b> Hanu the Trickster's The Ultimate Trick (rulebook p.16, milestone 8.71b) hands the
    /// next action of a hero he targeted with Hurry up! to HIS seat: the host authorizes that seat's
    /// <c>StartAction</c>, <c>PerformHold</c>, <c>PerformFastTravel</c> and <c>PerformClear</c>, and
    /// refuses the targeted seat's. The screen did not follow. A printed action is drawn as an
    /// overlay region on the RESOLVING CARD inside the viewed hero's own tray
    /// (<see cref="DirectorHandsUnityView"/>), and a delegated hero's card is not in Hanu's hand - so
    /// on a seat-scoped HUD the authorized seat was offered no control at all, while the card that
    /// did carry the controls sat in the tray of the one seat the host refuses. The authority was
    /// right and the screen was wrong, and in real play the Ultimate could not be used.</para>
    ///
    /// <para><b>The sentence this class states once.</b> A seat draws the selected actor's printed
    /// controls exactly when that seat's hero is the hero DECIDING that action - which is the actor
    /// themselves on every turn no delegation stands over, and Hanu while one does. Everything else
    /// follows from it: the deciding seat is given the delegated hero's revealed resolving card to
    /// draw them on (<see cref="DirectorHandsPresenter"/>'s "deciding for X" slot), the targeted seat
    /// draws its own card dark and presses nothing, and the narration names both heroes.</para>
    ///
    /// <para><b>Nothing here is new authority and nothing here crosses the wire.</b> Every answer is
    /// <see cref="DelegatedActionQuery"/>'s, read off the projection this viewer already holds: both
    /// the rewritten card and the unlocked Ultimate sit in rulebook p.4 OPEN zones, so every seat
    /// derives the same delegation from the frame it already receives - milestone 8.71b's own
    /// reasoning, asserted there. This class adds the VIEWER to that question and nothing else.</para>
    /// </summary>
    public static class DelegatedActionPresentation
    {
        /// <summary>
        /// The word a "deciding for X" slot wears, so the card can never be mistaken for one of the
        /// viewer's own hand cards.
        /// </summary>
        public const string DecidingForPrefix = "DECIDING FOR ";

        /// <summary>The activity the deciding seat's own narration names beside both heroes.</summary>
        public const string DecidingActivity = "CHOOSE AN ACTION, OR END THE TURN";

        /// <summary>The activity every other seat's narration names beside both heroes.</summary>
        public const string WaitingActivity = "CHOOSING THE ACTION";

        /// <summary>
        /// The delegation standing over the selected actor's own action right now, or
        /// <see cref="DelegatedAction.None"/>.
        /// </summary>
        public static DelegatedAction Standing(IMatchReadModel projection) =>
            projection == null || !projection.IsTurnActive || !projection.SelectedActor.IsDefined
                ? DelegatedAction.None
                : DelegatedActionQuery.ForActor(
                    projection.Definitions,
                    projection.Cards,
                    projection.Heroes,
                    projection.SelectedActor.ActorId);

        /// <summary>
        /// Whose seat is choosing the selected actor's action - the actor themselves on every turn no
        /// delegation stands over, and the deciding hero while one does. Undefined when no turn is
        /// running, which is the one thing <see cref="DelegatedActionQuery.DecidingHeroFor"/> cannot
        /// say on its own because it is asked about an actor rather than about a projection.
        /// </summary>
        public static HeroId DecidingHeroFor(IMatchReadModel projection)
        {
            if (projection == null || !projection.SelectedActor.IsDefined)
            {
                return default;
            }

            var delegation = Standing(projection);
            return delegation.IsDefined
                ? delegation.DecidingHeroId
                : projection.SelectedActor.ActorId;
        }

        /// <summary>
        /// The delegation this viewer is the one DECIDING, or <see cref="DelegatedAction.None"/>.
        /// </summary>
        /// <remarks>
        /// A viewer that names no hero is the Director sandbox's seatless view, which answers for
        /// everybody at once - so it is handed the slot for whichever hero's tray it is pointed at,
        /// exactly as it is handed every other seat's controls. That is what keeps the sandbox honest
        /// rather than privileged: pointed at the targeted hero it draws what that player would see,
        /// and pointed at the deciding hero it draws what Hanu's player would see.
        /// </remarks>
        public static DelegatedAction DecidedBy(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var delegation = Standing(projection);
            return delegation.IsDefined && viewerHeroId.IsDefined &&
                delegation.DecidingHeroId.Equals(viewerHeroId) &&
                !delegation.DelegatedHeroId.Equals(viewerHeroId)
                    ? delegation
                    : DelegatedAction.None;
        }

        /// <summary>
        /// Whether this viewer draws the selected actor's printed-action controls.
        ///
        /// <para>True for the hero who decides the action and for nobody else, so a control is drawn
        /// exactly where the host would accept its command. A viewer naming no hero is the Director's
        /// seatless view and reads every branch as its own, which is how every pre-8.72 fixture and
        /// journey that composes a viewless model keeps reading.</para>
        /// </summary>
        public static bool DrawsTheActionControls(IMatchReadModel projection, HeroId viewerHeroId)
        {
            if (projection == null || !projection.IsTurnActive ||
                !projection.SelectedActor.IsDefined || !viewerHeroId.IsDefined)
            {
                return true;
            }

            return DecidingHeroFor(projection).Equals(viewerHeroId);
        }

        /// <summary>
        /// Whether this viewer's own piece is acting while somebody else decides - the targeted
        /// player's half of the same sentence, whose tray draws the card dark and presses nothing.
        /// </summary>
        public static bool IsDecidedElsewhere(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var delegation = Standing(projection);
            return delegation.IsDefined && viewerHeroId.IsDefined &&
                delegation.DelegatedHeroId.Equals(viewerHeroId);
        }

        /// <summary>The slot's own word, built from the projected hero display name.</summary>
        public static string DecidingForLabel(string delegatedHeroDisplayName) =>
            DecidingForPrefix + (delegatedHeroDisplayName ?? string.Empty).ToUpperInvariant();
    }
}

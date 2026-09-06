using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z4. A presenter whose output depends on WHICH SEAT is looking - the one question
    /// "is this decision mine?" that every seat-scoped surface has to ask, and that only a viewer
    /// identity can answer.
    ///
    /// <para><b>Why this interface exists at all.</b> A networked client cannot know its hero at
    /// composition time. <c>ConfiguredMatchSceneRuntime</c> builds every presenter in its
    /// constructor, but the seat is not granted until the lobby admits the player, so each of these
    /// presenters is constructed with <c>Seat?.HeroId ?? default</c> - the UNDEFINED hero - and has
    /// to be told the truth afterwards, in <c>AttachOnlineClient</c>. The undefined hero is not a
    /// neutral placeholder: it is the DIRECTOR'S identity, and the Director legitimately owns every
    /// decision at once, so every "is this mine?" predicate reads an unrepaired seat as "yes,
    /// everything is mine". That is why the first live internet playtest saw every hero's turn
    /// narrated in the first person, the gold awaited-bar lit through other players' commitments, a
    /// greyed END TURN drawn on seats that were not acting, and the board-mark pulse mis-branched.
    /// </para>
    ///
    /// <para><b>Why an interface rather than three more repair lines.</b> Before this milestone the
    /// repair existed for exactly one of the three presenters that needed it - the hand tray - and
    /// the other two stored their identity in <c>readonly</c> fields that could not be told later
    /// even in principle. Nothing anywhere said the set had members, so the two that were missed
    /// were missed silently. Implementing this interface is now the declaration "I consume a viewer
    /// identity", <c>ConfiguredMatchSceneRuntime.SeatScopedPresenters</c> is the one list that
    /// answers "who must be told", and <c>SeatScopedPresenterBindingTests</c> compares the two so a
    /// FOURTH such presenter cannot be added and forgotten the same way.</para>
    /// </summary>
    public interface ISeatScopedPresenter
    {
        /// <summary>
        /// Binds this presenter's viewer identity to the admitted seat's hero, discarding whatever
        /// per-projection cache the presenter holds so the next refresh is rebuilt against the new
        /// identity rather than served from a model composed under the old one.
        /// </summary>
        /// <returns>
        /// <c>false</c> when <paramref name="heroId"/> is undefined or is absent from this match's
        /// declared setup - the same refusal <c>DirectorHandsPresenter.SetHomeHero</c> has always
        /// made, so a caller cannot bind a seat this build does not have.
        /// </returns>
        bool SetHomeHero(HeroId heroId);

        /// <summary>
        /// The viewer identity currently bound - the admitted seat's hero on a seat-scoped runtime,
        /// and the undefined hero on the Director's, whose runtime legitimately owns every decision
        /// at once. Exposed so the binding can be asserted directly rather than reflected out of a
        /// private field: "after admission, EVERY seat-scoped presenter reports the admitted seat's
        /// hero" is the assertion the composition defect above would have failed.
        /// </summary>
        HeroId HomeHeroId { get; }
    }
}

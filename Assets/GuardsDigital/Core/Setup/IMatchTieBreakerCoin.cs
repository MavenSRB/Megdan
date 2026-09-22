using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Milestone 8.75a. The match's one physical Tie Breaker coin, as the card-action dispatcher is
    /// allowed to see it: which face is showing, and - for the one printed sentence that turns it
    /// over mid-turn - the ability to turn it over.
    ///
    /// <para><b>Why the dispatcher is bound to a live coin rather than handed a value.</b>
    /// <see cref="TieBreakerState"/> is a value, and a value passed into a call is the face the coin
    /// was showing when the call started. Ignatia the Mad's whole set branches on the face
    /// (<c>ignatia-coin-sun@1</c>), and her Ultimate flips it in the middle of a turn - so an offer
    /// probe, a repeated performance and a card-text page that all run AFTER that flip must read the
    /// new face, and every one of them is reached from a call that began before it. The binding is
    /// the same arrangement <c>CardActionResolutionAuthority.BindRosterState</c> and
    /// <c>BindHeroItems</c> already use for the other authoritative states the dispatcher reads: one
    /// live object, owned by the authority that holds it, read lazily rather than copied.</para>
    ///
    /// <para><b>The coin is authoritative round state and nothing here persists it.</b>
    /// <c>InitiativeResolutionAuthority</c> holds it, the round section of the authoritative snapshot
    /// already serializes it, and a restore re-binds this the way it re-binds the roster state. So a
    /// flip survives a snapshot, a replay and a seat Undo without a byte being added to the payload -
    /// which is why schema v44 does not move for it.</para>
    /// </summary>
    public interface IMatchTieBreakerCoin
    {
        /// <summary>The face the coin is showing right now.</summary>
        TieBreakerState TieBreakerState { get; }

        /// <summary>
        /// Turns the coin over and reports the face it now shows. Rulebook p.5 gives the coin two
        /// faces, so this is total: it is never asked of an unrevealed coin, because a match always
        /// has one showing from setup onwards.
        /// </summary>
        TeamSide FlipTieBreaker();
    }
}

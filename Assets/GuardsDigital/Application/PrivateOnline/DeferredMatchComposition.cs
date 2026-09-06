using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35z37. What builds a match when the lobby says to, rather than when the
    /// scene loads.
    /// </summary>
    /// <remarks>
    /// <para><b>The wall this exists to remove, stated exactly.</b> The configured scene composed its
    /// board in <c>Awake</c> - before any host, client or lobby driver was attached - so both of the
    /// decisions a lobby is FOR, the printed format and who plays which hero, were settled before
    /// anybody could make them. Milestone 8.35z16 recorded that wall in its own non-goals and so did
    /// the hero selector; this is the seam both of them were waiting on, and it is deliberately ONE
    /// seam rather than two, because they are the same decision taken at the same moment.</para>
    ///
    /// <para><b>Why the dependency points this way.</b> Composing a match means building a board, a
    /// runtime and a set of views, which is scene work and cannot live here. What CAN live here is
    /// the moment: the session knows when the lobby ends, and it is the only thing that does. So the
    /// session declares the contract and calls it, and the scene implements it - the same inversion
    /// <c>IPrivateSessionOutbox</c> and <c>ITestDriverProcessLauncher</c> already use to keep sockets
    /// and OS processes out of this assembly.</para>
    ///
    /// <para><b>The contract's one hard requirement is the seats.</b> A recomposed match must seat
    /// exactly the seats the old one did - same ids, same sides, same order - because every live
    /// connection, epoch and reconnect capability the host holds is keyed by
    /// <see cref="SeatId"/>. That holds by construction rather than by care: seat ids are the
    /// positional player identities of the FORMAT, and a line-up never changes the format. The host
    /// checks it anyway and refuses loudly, because a seat table that silently moved would hand a
    /// player another player's hero.</para>
    /// </remarks>
    public interface IDeferredMatchComposition
    {
        /// <summary>
        /// Compose the match these heroes play, and return the host of it.
        /// </summary>
        /// <param name="lineup">
        /// The line-up the session settled on. Never null: a session with nothing chosen passes
        /// setup step 7's printed line-up, which composes exactly the board this build ships.
        /// </param>
        /// <returns>
        /// The composed match's host, or <c>null</c> to leave the session on the match it already
        /// serves. Returning null is the honest answer for a caller that composes nothing - a
        /// fixture, a rehearsal, a host whose scene has already composed what was asked for - and it
        /// is what keeps every path that predates this seam behaving exactly as it did.
        /// </returns>
        IMatchHost ComposeForLineup(HeroLineup lineup);
    }
}

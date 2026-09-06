using System;
using GuardsDigital.Application;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// What the online seat's connection notice draws, and whether it draws at all.
    /// </summary>
    /// <remarks>
    /// <para><b>Item 3 of the 2026-09-06 log drop-down refinements.</b> The user's design, recorded
    /// with a screenshot of the banner sitting across the open log's header: "a HEALTHY connection
    /// is the default state and earns no screen - show NOTHING when connected; surface only
    /// anomalous states (CONNECTING… / RECONNECTING… / WAITING FOR HOST) as a banner, and even those
    /// placed clear of the narration bar and the log drop-down".</para>
    /// <para><b>Presentation only.</b> Nothing here reads the wire, the projection or a seat's
    /// authority - the whole type is a function of the session facts
    /// <see cref="NetworkMatchFeedback"/> already words, deciding which of those words earn screen.
    /// The wording table itself is untouched, and <see cref="ConfiguredMatchHudRenderer.CalculateConnectionBannerRect"/>
    /// decides where.</para>
    /// <para><b>Why the predicate is structural rather than a string comparison.</b> Reading the
    /// composed sentence back and testing it against "Connected to the host." would make a wording
    /// change silently change which states are hidden. So the healthy case is stated in the same
    /// terms <see cref="NetworkMatchFeedback.ForState"/> branches on - and
    /// <c>MatchConnectionBannerTests</c> sweeps every state and flag combination asserting that the
    /// two agree exactly, which catches the drift without depending on it at runtime.</para>
    /// </remarks>
    public readonly struct MatchConnectionBannerViewModel : IEquatable<MatchConnectionBannerViewModel>
    {
        private MatchConnectionBannerViewModel(bool isShown, string text)
        {
            IsShown = isShown;
            Text = text ?? string.Empty;
        }

        /// <summary>The hidden banner: a healthy connection, or no online session at all.</summary>
        public static MatchConnectionBannerViewModel Hidden { get; } =
            new MatchConnectionBannerViewModel(false, string.Empty);

        /// <summary>An anomalous connection state, worded and shown.</summary>
        public static MatchConnectionBannerViewModel Shown(string text) =>
            new MatchConnectionBannerViewModel(true, text);

        /// <summary>Whether the banner is drawn at all.</summary>
        public bool IsShown { get; }

        /// <summary>What it says while it is drawn; empty while it is not.</summary>
        public string Text { get; }

        public bool Equals(MatchConnectionBannerViewModel other) =>
            IsShown == other.IsShown && string.Equals(Text, other.Text, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is MatchConnectionBannerViewModel other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (IsShown.GetHashCode() * 397) ^ (Text?.GetHashCode() ?? 0);
            }
        }
    }

    /// <summary>
    /// Item 3 of the 2026-09-06 log drop-down refinements: which connection states earn screen.
    /// </summary>
    public static class MatchConnectionBanner
    {
        /// <summary>
        /// The healthy connection - and the ONLY one. It is the exact branch
        /// <see cref="NetworkMatchFeedback.ForState"/> answers "Connected to the host." on: seated,
        /// holding a projection, waiting on nothing, and carrying no refusal feedback. Every other
        /// combination of those facts is something a player would want told.
        /// </summary>
        public static bool IsHealthy(
            PrivateSessionClientState state, bool hasProjection, bool waiting, string feedback) =>
            state == PrivateSessionClientState.Seated &&
            hasProjection &&
            !waiting &&
            string.IsNullOrEmpty(feedback);

        /// <summary>
        /// Whether this connection state earns a banner. The complement of
        /// <see cref="IsHealthy"/> - "a HEALTHY connection is the default state and earns no
        /// screen", so anomalous is simply everything else.
        /// </summary>
        public static bool EarnsScreen(
            PrivateSessionClientState state, bool hasProjection, bool waiting, string feedback) =>
            !IsHealthy(state, hasProjection, waiting, feedback);

        /// <summary>
        /// The whole banner decision for one seat: hidden while the connection is healthy, and
        /// otherwise the wording <see cref="NetworkMatchFeedback"/> already states for it, upper
        /// case the way every other standing HUD label reads.
        /// </summary>
        /// <param name="attached">
        /// Whether the online HUD is on screen at all. A lobby that has not handed over yet draws
        /// its own status in its own layout, so this banner is not the surface for it.
        /// </param>
        public static MatchConnectionBannerViewModel For(
            bool attached,
            bool matchStarted,
            PrivateSessionClientState state,
            bool reconnecting,
            bool hasProjection,
            bool waiting,
            string feedback,
            string rejection,
            bool delayed)
        {
            if (!attached || !matchStarted ||
                IsHealthy(state, hasProjection, waiting, feedback))
            {
                return MatchConnectionBannerViewModel.Hidden;
            }

            return MatchConnectionBannerViewModel.Shown(
                NetworkMatchFeedback
                    .ForState(state, reconnecting, hasProjection, waiting, feedback, rejection, delayed)
                    .ToUpperInvariant());
        }
    }
}

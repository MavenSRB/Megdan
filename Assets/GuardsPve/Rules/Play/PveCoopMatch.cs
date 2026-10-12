using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Composition;

namespace GuardsDigital.Pve.Play
{
    /// <summary>PVE-D28. One authority, three owner-private player seats, host-owned enemies and team choices.</summary>
    public sealed class PveCoopMatch
    {
        private const string FormatPrefix = "pve-coop-3-";
        public PveCoopMatch(PveComposedEncounter encounter)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            if (encounter.Heroes.Count != PveEncounterComposer.RequiredPlayerHeroCount)
                throw new ArgumentException("Co-op requires exactly three heroes.", nameof(encounter));
            var authority = MatchHost.Compose(encounter.InitialState);
            Session = new PveEncounterSession(encounter, authority, new LocalDirectorMatchClient(authority));
            Host = new MatchHost(Session, new MatchSeatPolicy(TeamSide.Blue, TeamSide.Red, PlayerCommands),
                encounter.InitialState.Experiments);
        }

        public PveComposedEncounter Encounter { get; }
        public PveEncounterSession Session { get; }
        public MatchHost Host { get; }

        public static PrivateSessionIdentity Identity(PrivateSessionIdentity content, ulong seed, string catalog, string encounter) =>
            new PrivateSessionIdentity(content.ProtocolFamily, content.ProtocolVersion, content.BuildIdentity,
                content.SnapshotSchemaVersion, content.MapId, content.MapContentRevision,
                FormatPrefix + seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                PrivateSessionContentDigest.HashText(content.ContentDigest + catalog + encounter));

        /// <summary>A public setup hint only; a fresh keyed admission still verifies the complete identity.</summary>
        public static bool TryReadHostSeed(SessionRejectMessage rejection, ulong currentSeed, out ulong hostSeed,
            bool includeMapMismatch = false)
        {
            hostSeed = 0;
            if (rejection == null || (rejection.Reason != SessionRejectionReason.MatchFormatMismatch &&
                !(includeMapMismatch && rejection.Reason == SessionRejectionReason.MapRevisionMismatch)) ||
                !rejection.HostFormatId.StartsWith(FormatPrefix, StringComparison.Ordinal)) return false;
            var text = rejection.HostFormatId.Substring(FormatPrefix.Length);
            if (!ulong.TryParse(text, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed == 0 ||
                parsed == currentSeed || parsed.ToString(System.Globalization.CultureInfo.InvariantCulture) != text) return false;
            hostSeed = parsed;
            return true;
        }

        public static bool IsSharedChoice(IMatchReadModel source)
        {
            var pending = source.PendingSystemTransitionStep;
            if (pending != null && MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _)) return false;
            return source.DecisionOwner.IsDefined && source.DecisionOwner.IsTeamOwned;
        }

        private IReadOnlyList<DirectorCommand> PlayerCommands(MatchSessionProjection source, MatchSeat seat,
            IReadOnlyList<DirectorCommand> commands)
        {
            var result = commands.Where(command => Session.Refusal(command) == null && Owns(source, seat, command)).ToList();
            return result.AsReadOnly();
        }

        private static bool Owns(MatchSessionProjection source, MatchSeat seat, DirectorCommand command)
        {
            if (command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                ModeRoundStartPlacementRules.TryReadItem(command.SystemTransitionChoice.ItemId, out var placed))
                return seat.Owns(placed);
            // Hero-owned decisions remain private. Team-owned choices are presented only by the host overlay.
            if (IsSharedChoice(source)) return false;
            return true;
        }
    }
}

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
        public PveCoopMatch(PveComposedEncounter encounter)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            if (encounter.Heroes.Count != PveEncounterComposer.RequiredPlayerHeroCount)
                throw new ArgumentException("Co-op requires exactly three heroes.", nameof(encounter));
            var authority = MatchHost.Compose(encounter.InitialState);
            Session = new PveEncounterSession(encounter, authority, new LocalDirectorMatchClient(authority));
            Host = new MatchHost(Session, new MatchSeatPolicy(TeamSide.Blue, TeamSide.Red, PlayerCommands));
        }

        public PveComposedEncounter Encounter { get; }
        public PveEncounterSession Session { get; }
        public MatchHost Host { get; }

        public static PrivateSessionIdentity Identity(PrivateSessionIdentity content, ulong seed, string catalog, string encounter) =>
            new PrivateSessionIdentity(content.ProtocolFamily, content.ProtocolVersion, content.BuildIdentity,
                content.SnapshotSchemaVersion, content.MapId, content.MapContentRevision,
                "pve-coop-1-" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                PrivateSessionContentDigest.HashText(content.ContentDigest + catalog + encounter));

        public static bool IsSharedChoice(IMatchReadModel source)
        {
            var pending = source.PendingSystemTransitionStep;
            if (pending != null && (MatchModeRules.TryReadPlacementRound(pending.DefinitionId, out _) ||
                MatchModeRules.TryReadLevelGrantHero(pending.DefinitionId, out _))) return false;
            return source.DecisionOwner.IsDefined && source.DecisionOwner.IsTeamOwned;
        }

        private IReadOnlyList<DirectorCommand> PlayerCommands(MatchSessionProjection source, MatchSeat seat,
            IReadOnlyList<DirectorCommand> commands)
        {
            var result = commands.Where(command => Session.Refusal(command) == null && Owns(source, seat, command)).ToList();
            if (source.PendingSystemTransitionStep == null && source.TurnIndex == 1 &&
                source.Phase == GuardsDigital.Core.Cards.RoundResolutionPhase.AwaitingCommitments)
            {
                var grant = DirectorCommand.BeginSystemTransition(MatchModeRules.LevelGrantTransitionId(seat.HeroId));
                if (Session.Refusal(grant) == null) result.Add(grant);
            }
            return result.AsReadOnly();
        }

        private static bool Owns(MatchSessionProjection source, MatchSeat seat, DirectorCommand command)
        {
            if (command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                ModeRoundStartPlacementRules.TryReadItem(command.SystemTransitionChoice.ItemId, out var placed))
                return seat.Owns(placed);
            // Hero-owned decisions remain private. Team-owned choices are presented only by the host overlay.
            if (source.PendingSystemTransitionStep != null && MatchModeRules.TryReadLevelGrantHero(
                source.PendingSystemTransitionStep.DefinitionId, out var leveled)) return seat.Owns(leveled);
            if (IsSharedChoice(source)) return false;
            return true;
        }
    }
}

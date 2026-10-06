using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
namespace GuardsDigital.Application
{
    public sealed class BoardChoiceVote
    {
        public BoardChoiceVote(HeroId voter, HexCoordinate position, DirectorCommand command)
        { Voter=voter; Position=position; Command=command ?? throw new ArgumentNullException(nameof(command)); }
        public HeroId Voter { get; }
        public HexCoordinate Position { get; }
        public DirectorCommand Command { get; }
    }
    public interface IBoardVoteClient { bool SubmitBoardVote(DirectorCommand command); }
    public static class SharedBoardChoice
    {
        public static bool RequiresVote(IMatchReadModel projection,DirectorCommand command) =>
            command.Kind==DirectorCommandKind.AnswerSystemTransitionStep &&
            projection.PendingSystemTransitionDescription?.Kind==SystemTransitionPromptKind.LanePushRespawnPlace ||
            command.Kind==DirectorCommandKind.ChooseCorrectionDestination && !projection.SelectedActor.IsDefined;
        public static HexCoordinate Position(IMatchReadModel p,DirectorCommand c) =>
            c.Kind==DirectorCommandKind.ChooseCorrectionDestination ? c.Coordinate :
            p.PendingSystemTransitionDescription.Options.Single(o=>o.OptionId.Equals(c.SystemTransitionChoice.OptionId)).Destination;
    }
}

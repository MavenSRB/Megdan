using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.4. A card-action dispatcher for a fixture that composes no round authority, carrying
    /// the two states rulebook p.13's hero-Defeat consequences read and write.
    ///
    /// It exists because the dispatcher refuses to resolve a hero Defeat it cannot price, and a fixture
    /// that drives an Attack to its outcome without a round has to state the Levels and Life counters
    /// itself rather than have them invented. The values are the ones a round authority substitutes
    /// when its composition supplies no setup - p.15's starting Level for every hero, and the prototype
    /// baseline's counters for every team - so a fixture that composes this and a fixture that composes
    /// a board-free round are answering with the same numbers.
    /// </summary>
    internal static class DefeatConsequenceComposition
    {
        public static CardActionResolutionAuthority Dispatcher(
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            MarkerAttachmentRegister markers = null)
        {
            return new CardActionResolutionAuthority(
                MatchContentCatalogs.ForThisBuild(),
                board,
                markers,
                new HeroProgressionState(participants),
                new TeamLifeState(participants, RoundResolutionAuthority.BaselineLifeCountersPerTeam));
        }
    }
}

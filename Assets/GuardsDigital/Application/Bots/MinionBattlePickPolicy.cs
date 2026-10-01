using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// Milestone 9.29. How a bot seat takes part in a Minion Battle whose choosing players each pick
    /// their own minions and the removal happens when every pick matches.
    /// </summary>
    /// <remarks>
    /// <para><b>A bot follows and never leads a human.</b> The user's reading of the playtest was that
    /// players could not tell why the board changed under them; a bot that picked first would be a
    /// teammate changing nothing on their board but still the one they would have to match. So a bot
    /// copies the complete picks of the first human participant, one statement at a time, and waits
    /// while no human has finished.</para>
    /// <para><b>Convergence, not cleverness</b> - the same rule the Initiative agreement has always
    /// used here. A team of bots only picks the first offered unit until its set is complete, and
    /// because every bot of that team sees the same candidates in the same order, every one of them
    /// arrives at the same set.</para>
    /// </remarks>
    public static class MinionBattlePickPolicy
    {
        /// <summary>
        /// The one statement that moves this bot's picks towards the set it should hold, or
        /// <c>null</c> when it already holds it or should wait.
        /// </summary>
        public static TeamAgreementCommand NextStatement(
            TeamAgreementProjection agreement, HeroId self, Func<HeroId, bool> isBot)
        {
            if (agreement == null || agreement.Kind != TeamAgreementKind.MinionBattle ||
                agreement.AuthorizedCommands.Count == 0 || isBot == null)
            {
                return null;
            }

            var own = agreement.PicksOf(self);
            var humans = agreement.Participants.Where(participant => !isBot(participant.HeroId)).ToArray();
            IReadOnlyList<RuleObject> target;
            if (humans.Length > 0)
            {
                var leader = humans.FirstOrDefault(participant => participant.PicksComplete);
                if (leader == null)
                {
                    return null;
                }

                target = leader.Picks;
            }
            else
            {
                var finished = agreement.Participants.FirstOrDefault(participant => participant.PicksComplete);
                if (finished == null)
                {
                    return own.Count < agreement.RequiredSelectionCount
                        ? agreement.AuthorizedCommands.FirstOrDefault(command =>
                            command.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval)
                        : null;
                }

                target = finished.Picks;
            }

            if (own.Count == target.Count && own.All(target.Contains))
            {
                return null;
            }

            // Take back a pick the target does not hold, where taking it back is offered; otherwise
            // add one it does hold - on a full set that replaces this bot's oldest pick.
            foreach (var unit in own.Where(unit => !target.Contains(unit)))
            {
                var unpick = Offered(agreement, TeamAgreementCommandKind.UnpickMinionBattleRemoval, unit);
                if (unpick != null)
                {
                    return unpick;
                }
            }

            foreach (var unit in target.Where(unit => !own.Contains(unit)))
            {
                var pick = Offered(agreement, TeamAgreementCommandKind.PickMinionBattleRemoval, unit);
                if (pick != null)
                {
                    return pick;
                }
            }

            return null;
        }

        private static TeamAgreementCommand Offered(
            TeamAgreementProjection agreement, TeamAgreementCommandKind kind, RuleObject unit) =>
            agreement.AuthorizedCommands.FirstOrDefault(command =>
                command.Kind == kind && command.Unit.Equals(unit));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Derives the Minion-Battle rules applicable to this round's battle from the unlocked Ultimates
    /// and the board.
    ///
    /// It is the twin of <see cref="UltimateTriggeredEffectSourceQuery"/> and owns the same one thing:
    /// p.16's shared on-board lifetime - "always in effect (as long as your hero is on the board)".
    /// What the rule then does stays in the trusted declaration and its registered handler, and where
    /// the hero is standing is a fact this query reads once and hands over, so no handler needs the
    /// board to answer with.
    ///
    /// A hero off the board supplies nothing, which is p.16's own sentence rather than a Minion-Battle
    /// rule of its own; a hero on the board but outside the Battle Zone is derived and left to the
    /// handler, because "in the Battle Zone" is p.8's condition and belongs to the rule that reads it.
    /// </summary>
    public static class MinionBattleRuleSourceQuery
    {
        public static IReadOnlyList<MinionBattleParticipant> Derive(
            UnlockedUltimateSet ultimates,
            IBoardDisplacementAuthority board,
            MatchParticipants participants)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));

            var derived = new List<MinionBattleParticipant>();
            foreach (var ultimate in ultimates.Ultimates)
            {
                var declaration = ultimate.Rule.MinionBattleRule;
                if (declaration == null)
                {
                    continue;
                }

                if (!board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(ultimate.OwnerId), out var coordinate))
                {
                    continue;
                }

                derived.Add(new MinionBattleParticipant(
                    declaration,
                    ultimate.OwnerId,
                    participants.GetTeamForHero(ultimate.OwnerId).Side,
                    BattleZoneQuery.IsInCurrentBattleZone(board, coordinate)));
            }

            return Array.AsReadOnly(derived
                .OrderBy(participant => participant.HeroId.Value, StringComparer.Ordinal)
                .ToArray());
        }
    }
}

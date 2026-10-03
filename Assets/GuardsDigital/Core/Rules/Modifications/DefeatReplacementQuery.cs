using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>A would-be minion Defeat replaced before its reward, history, or removal exists.</summary>
    public static class DefeatReplacementQuery
    {
        public static IReadOnlyList<AppliedRuleModification> Sources(
            RuleObject subject, RuleModificationSet modifications)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            return Array.AsReadOnly(modifications.For(subject).Where(source =>
                source.Modification.Kind == RuleModificationKind.DefeatReplacement).ToArray());
        }

        public static bool TryApply(
            BoardObjectReference subject, RuleActivity activity, RuleObject actor,
            IBoardDisplacementAuthority board, MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (subject.Kind != BoardObjectKind.Minion ||
                !board.TryGetObjectCoordinate(subject, out _) ||
                TargetQuery.IsPrevented(subject.AsRuleObject(), activity, actor, board, participants, modifications))
                return false;

            foreach (var source in Sources(subject.AsRuleObject(), modifications))
            {
                if (!(board is ITokenPlacementAuthority tokens))
                    throw new InvalidOperationException("A token-priced Defeat replacement requires token authority.");
                var token = tokens.PlacedTokens.SingleOrDefault(candidate =>
                    candidate.PropertySources.Any(property =>
                        string.Equals(property.SourceId, source.SourceId, StringComparison.Ordinal)));
                // One command can settle several Defeats against the same derived set. A token
                // already spent by the first replacement cannot pay for a later one.
                if (token == null) continue;
                // This is the source paying its own mandatory price, not the enemy removing an
                // immune token with an action. Reclamation preserves supply and placement identity.
                tokens.CommitValidatedTokenReclamation(token.Id);
                return true;
            }
            return false;
        }
    }
}

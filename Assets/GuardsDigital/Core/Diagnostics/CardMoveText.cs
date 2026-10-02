using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Diagnostics
{
    /// <summary>
    /// Milestone 9.32g. The one text form of a list of <see cref="CardMoveFact"/>s, written into a
    /// rule-event log entry by <see cref="RuleEventLogProjection"/> and read back by
    /// <see cref="MatchLogProjection"/>: <c>definition/From/To/flags/Colour</c> per move, the moves
    /// joined by commas. The flags are <c>d</c> (departs facedown) and <c>a</c> (arrives facedown), or
    /// <c>-</c>. A definition id never holds a slash, a comma, a semicolon or an equals sign, which are
    /// the separators of this form and of the entry's own fields.
    /// </summary>
    public static class CardMoveText
    {
        public static string Encode(IEnumerable<CardMoveFact> moves) =>
            string.Join(",", (moves ?? Enumerable.Empty<CardMoveFact>()).Select(move =>
                (move.Definition.IsDefined ? move.Definition.Value : string.Empty) + "/" +
                move.From + "/" + move.To + "/" +
                ((move.DepartsFacedown ? "d" : string.Empty) + (move.ArrivesFacedown ? "a" : string.Empty) is var flags &&
                    flags.Length > 0 ? flags : "-") + "/" +
                move.KnownColor));

        public static IReadOnlyList<CardMoveFact> Decode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<CardMoveFact>();
            }

            var moves = new List<CardMoveFact>();
            foreach (var part in text.Split(','))
            {
                var fields = part.Split('/');
                if (fields.Length != 5 ||
                    !Enum.TryParse(fields[1], false, out CardMoveZone from) ||
                    !Enum.TryParse(fields[2], false, out CardMoveZone to) ||
                    !Enum.TryParse(fields[4], false, out CardColor color))
                {
                    throw new FormatException(
                        "A recorded card move reads 'definition/From/To/flags/Colour', not '" + part + "'.");
                }

                moves.Add(new CardMoveFact(
                    fields[0].Length == 0 ? default : new CardDefinitionId(fields[0]),
                    from,
                    to,
                    departsFacedown: fields[3].IndexOf('d') >= 0,
                    arrivesFacedown: fields[3].IndexOf('a') >= 0,
                    knownColor: color));
            }

            return moves;
        }
    }
}

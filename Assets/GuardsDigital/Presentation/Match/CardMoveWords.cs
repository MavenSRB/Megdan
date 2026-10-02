using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.32g. The words of the match-log rows a card swap and an Ultimate's card movement
    /// write (approved mockup F). A move names its card through the definition its row carries, which
    /// the seat projection has already cleared where this seat may not read it; an unnamed card is
    /// told by its zone and the colour its text fixes - "THE GOLD CARD IN HAND", "A FACEDOWN CARD",
    /// "TWO GOLD CARDS". No pronouns: the hero is always named.
    /// </summary>
    public static class CardMoveWords
    {
        /// <summary>
        /// "TAKAHIDE SWAPPED STING LIKE A BEE FOR STRIKE LIKE A TIGER FROM THE DECK",
        /// "TAKAHIDE SWAPPED THE GOLD CARD IN HAND WITH ONE FROM THE DECK",
        /// "WASP SWAPPED ELECTROCUTE FOR SHOCK FROM THE DISCARD (HOLD MY SAKE)".
        /// </summary>
        public static string Swapped(
            IReadOnlyList<CardDefinition> definitions, string owner, IReadOnlyList<CardMoveFact> moves, string sourceSuffix)
        {
            if (moves == null || moves.Count < 2)
            {
                return owner + " SWAPPED A CARD" + sourceSuffix;
            }

            var outgoing = moves[0];
            var incoming = moves[1];
            var outName = NameOrNull(definitions, outgoing);
            string outPhrase;
            switch (outgoing.From)
            {
                case CardMoveZone.Hand:
                    outPhrase = (outName ?? TheColourCard(outgoing)) + " IN HAND";
                    break;
                case CardMoveZone.DiscardPile:
                    outPhrase = outName != null
                        ? "THE DISCARDED " + outName
                        : TheColourCard(outgoing) + " IN THE DISCARD";
                    break;
                default:
                    outPhrase = outName ?? TheColourCard(outgoing);
                    break;
            }

            var inName = NameOrNull(definitions, incoming);
            string inPhrase;
            if (incoming.ArrivesFacedown)
            {
                inPhrase = inName != null ? "FOR " + inName + ", FACEDOWN" : "FOR A FACEDOWN CARD";
            }
            else
            {
                var from = incoming.From == CardMoveZone.Deck
                    ? " FROM THE DECK"
                    : incoming.From == CardMoveZone.DiscardPile ? " FROM THE DISCARD" : string.Empty;
                inPhrase = inName != null ? "FOR " + inName + from : "WITH ONE" + from;
            }

            return owner + " SWAPPED " + outPhrase + " " + inPhrase + sourceSuffix;
        }

        /// <summary>
        /// "BUSHIDO TO THE DECK, STING LIKE A BEE AND STRIKE LIKE A TIGER TO HAND", or on a seat that
        /// may not name them "THE SILVER CARD TO THE DECK, TWO GOLD CARDS TO HAND" - the moves grouped
        /// by where they went, in the order they moved.
        /// </summary>
        public static string Grouped(IReadOnlyList<CardDefinition> definitions, IReadOnlyList<CardMoveFact> moves)
        {
            var groups = new List<string>();
            foreach (var destination in (moves ?? Array.Empty<CardMoveFact>()).Select(move => move.To).Distinct())
            {
                var going = moves.Where(move => move.To == destination).ToArray();
                var parts = going.Select(move => NameOrNull(definitions, move)).Where(name => name != null).ToList();
                foreach (var unnamed in going.Where(move => NameOrNull(definitions, move) == null)
                    .GroupBy(move => move.KnownColor))
                {
                    var count = unnamed.Count();
                    var colour = unnamed.Key == CardColor.None ? string.Empty : unnamed.Key.ToString().ToUpperInvariant() + " ";
                    parts.Add(count == 1 ? "THE " + colour + "CARD" : CountWord(count) + " " + colour + "CARDS");
                }

                groups.Add(JoinWithAnd(parts) + " " + ToWords(destination));
            }

            return string.Join(", ", groups);
        }

        /// <summary>The move's card by name, or null where this seat's row leaves it unnamed.</summary>
        public static string NameOrNull(IReadOnlyList<CardDefinition> definitions, CardMoveFact move) =>
            move.Definition.IsDefined
                ? (definitions ?? Array.Empty<CardDefinition>())
                    .FirstOrDefault(definition => definition.Id.Equals(move.Definition))?.DisplayName?.ToUpperInvariant()
                : null;

        private static string TheColourCard(CardMoveFact move) =>
            move.KnownColor == CardColor.None
                ? "A CARD"
                : "THE " + move.KnownColor.ToString().ToUpperInvariant() + " CARD";

        private static string ToWords(CardMoveZone zone)
        {
            switch (zone)
            {
                case CardMoveZone.Deck: return "TO THE DECK";
                case CardMoveZone.Hand: return "TO HAND";
                case CardMoveZone.DiscardPile: return "TO THE DISCARD";
                default: return "INTO PLAY";
            }
        }

        private static string JoinWithAnd(IReadOnlyList<string> parts) =>
            parts.Count <= 1
                ? string.Concat(parts)
                : string.Join(", ", parts.Take(parts.Count - 1)) + " AND " + parts[parts.Count - 1];

        private static string CountWord(int count)
        {
            var words = new[] { "NO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX" };
            return count >= 0 && count < words.Length
                ? words[count]
                : count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

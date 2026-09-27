using System;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.112f. An Ultimate's declaration that it runs after its controller PLACES tokens of
    /// one kind (rulebook p.18's Place) - Mrak's Rock and a Hard Place, "Once per turn, after you place
    /// one or more Rock tokens into one or more spaces adjacent to one or more enemy heroes, each of
    /// those heroes discards a card, if able."
    ///
    /// <para>The sibling of <see cref="AfterPushTrigger"/>, and exclusive with it and every other
    /// trigger shape for the reason those are exclusive with each other: p.16 gives an Ultimate one
    /// permanent ability. It names the token KIND, because the sentence does ("Rock tokens"), and the
    /// subjects are always the enemy heroes adjacent to what that one placing step placed - one run of
    /// the triggered effect for each, once per turn.</para>
    /// </summary>
    public readonly struct AfterTokensPlacedTrigger : IEquatable<AfterTokensPlacedTrigger>
    {
        public AfterTokensPlacedTrigger(TokenDefinitionId tokenKind)
        {
            if (string.IsNullOrWhiteSpace(tokenKind.Value))
            {
                throw new ArgumentException(
                    "An after-tokens-placed trigger names the token kind it watches.",
                    nameof(tokenKind));
            }

            TokenKind = tokenKind;
        }

        public static AfterTokensPlacedTrigger None => default;

        public TokenDefinitionId TokenKind { get; }

        public bool IsDeclared => !string.IsNullOrWhiteSpace(TokenKind.Value);

        public bool Equals(AfterTokensPlacedTrigger other) => TokenKind.Equals(other.TokenKind);

        public override bool Equals(object obj) =>
            obj is AfterTokensPlacedTrigger other && Equals(other);

        public override int GetHashCode() => IsDeclared ? TokenKind.GetHashCode() : 0;

        public override string ToString() =>
            IsDeclared ? $"after placing {TokenKind.Value}" : "no after-tokens-placed trigger";
    }
}

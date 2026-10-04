using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The complete persisted state of a <see cref="TurnCardCommitmentAuthority"/>.
    /// </summary>
    /// <remarks>
    /// The card instances and participants a commitment is validated against are written
    /// elsewhere in the snapshot, so they stay separate required arguments on
    /// <see cref="TurnCardCommitmentAuthority.Restore"/>. Identity and duplicate validation
    /// remains in the authority, which owns the expected-hero set needed to judge it.
    /// </remarks>
    public sealed class TurnCardCommitmentRestoreState
    {
        public TurnCardCommitmentRestoreState(
            IEnumerable<KeyValuePair<HeroId, CardInstanceId>> commitments,
            IEnumerable<HeroId> passes,
            bool isRevealed,
            IEnumerable<KeyValuePair<HeroId, CardInstanceId>> secondCommitments = null)
        {
            Commitments = Array.AsReadOnly(
                (commitments ?? throw new ArgumentNullException(nameof(commitments))).ToArray());
            Passes = Array.AsReadOnly(
                (passes ?? throw new ArgumentNullException(nameof(passes))).ToArray());
            IsRevealed = isRevealed;
            SecondCommitments = Array.AsReadOnly((secondCommitments ??
                Enumerable.Empty<KeyValuePair<HeroId, CardInstanceId>>()).ToArray());
        }

        public IReadOnlyList<KeyValuePair<HeroId, CardInstanceId>> Commitments { get; }
        public IReadOnlyList<HeroId> Passes { get; }
        public bool IsRevealed { get; }
        public IReadOnlyList<KeyValuePair<HeroId, CardInstanceId>> SecondCommitments { get; }

        public void Deconstruct(
            out IReadOnlyList<KeyValuePair<HeroId, CardInstanceId>> commitments,
            out IReadOnlyList<HeroId> passes,
            out bool isRevealed)
        {
            commitments = Commitments;
            passes = Passes;
            isRevealed = IsRevealed;
        }
    }
}

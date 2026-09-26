using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.75b. The ONE place that knows which symbol the Tie Breaker coin's two faces bear,
    /// and therefore which of a two-text card's printed texts is live right now.
    ///
    /// <para><b>Why it is here and not in the hero's own content.</b> Two readers need this mapping and
    /// they may not reach each other. The content branches on it - Ignatia's cards compose one
    /// <c>Conditional</c> over the <c>ignatia-coin-sun@1</c> predicate - and the HUD draws it, and
    /// presentation is forbidden from referencing <c>Core.Cards.Verified</c> at all
    /// (<c>VerifiedContentIsolationConformanceTests</c>). A constant duplicated on both sides would be
    /// two statements that could disagree about which face is which, silently, on a card whose whole
    /// identity is that question. So the mapping is stated once here, in non-Verified Core, and the
    /// hero's content reads it: <c>IgnatiaComponentContent.SunFace</c> and <c>MoonFace</c> are this
    /// type's <see cref="SunSide"/> and <see cref="MoonSide"/>.</para>
    ///
    /// <para><b>USER-CORRECTED 2026-09-26 from live play:</b> the prior colour reading was inverted.
    /// The coin's sun face is the one that reveals <see cref="TeamSide.Blue"/> and its moon face the
    /// one that reveals <see cref="TeamSide.Red"/>. The engine models the coin only as a revealed side
    /// (<see cref="TieBreakerState.RevealedSide"/>); the symbols are what is physically printed on
    /// those sides.</para>
    ///
    /// <para><b>It decides nothing and stores nothing.</b> Every member is a pure function of arguments
    /// the caller already holds, so it is equally available to the authority, to an offline fixture and
    /// to a networked seat drawing a card, and it can never become a second opinion about a rule.</para>
    /// </summary>
    public static class CoinSelectedTextQuery
    {
        /// <summary>The side revealed by the coin face bearing the sun (☀) symbol. USER-CORRECTED.</summary>
        public static TeamSide SunSide => TeamSide.Blue;

        /// <summary>The side revealed by the coin face bearing the moon (☾) symbol. USER-CORRECTED.</summary>
        public static TeamSide MoonSide => TeamSide.Red;

        /// <summary>
        /// The symbol printed on the revealed coin face, and <see cref="PrintedTextFace.None"/> for an
        /// unrevealed coin - which is a real state (<see cref="TieBreakerState.Unrevealed"/>) and not a
        /// third symbol, so neither printed text is live and nothing may guess one.
        /// </summary>
        public static PrintedTextFace FaceOf(TeamSide revealedSide) =>
            revealedSide == SunSide ? PrintedTextFace.Sun
            : revealedSide == MoonSide ? PrintedTextFace.Moon
            : PrintedTextFace.None;

        /// <summary>The live face of a match's coin.</summary>
        public static PrintedTextFace LiveFace(TieBreakerState tieBreaker) =>
            FaceOf(tieBreaker.RevealedSide);

        /// <summary>
        /// The side whose coin face bears this symbol - the inverse of <see cref="FaceOf"/>, and
        /// <see cref="TeamSide.None"/> for <see cref="PrintedTextFace.None"/>. A surface uses it to
        /// say WHICH side the coin is showing while it names the live text.
        /// </summary>
        public static TeamSide SideShowing(PrintedTextFace face) =>
            face == PrintedTextFace.Sun ? SunSide
            : face == PrintedTextFace.Moon ? MoonSide
            : TeamSide.None;

        /// <summary>The other symbol; <see cref="PrintedTextFace.None"/> has no opposite.</summary>
        public static PrintedTextFace Opposite(PrintedTextFace face) =>
            face == PrintedTextFace.Sun ? PrintedTextFace.Moon
            : face == PrintedTextFace.Moon ? PrintedTextFace.Sun
            : PrintedTextFace.None;

        /// <summary>
        /// Whether <paramref name="heroId"/> may currently apply either printed text regardless of the
        /// coin, because a card they control that declares
        /// <see cref="PrintedTextSelection.FreesTheCoinChoice"/> is sourcing a live Active effect.
        ///
        /// <para><b>Every input is a fact a seat already holds, which is the point.</b>
        /// <c>IMatchReadModel.ActiveEffectSourceCardInstanceIds</c> is the card-level live-effect list
        /// the trays have drawn <c>ACTIVE</c> from since milestone 8.5b, a <see cref="CardInstance"/>
        /// names its owner, and the definition it names carries the declaration. Nothing here reads an
        /// <c>ActiveEffectInstance</c> - authority-side state a networked seat is never sent - and
        /// nothing needs a capability catalog threaded to it, so the answer is the same on the
        /// Director, on a bot seat and on a remote client.</para>
        ///
        /// <para>A missing list, card or definition answers <c>false</c>: a seat that was not shown a
        /// card simply does not know of a freedom it grants, which is rulebook p.4's open-information
        /// boundary rather than a hole. In practice the grant is public - the card is in its owner's
        /// turn area, faceup, for the round it applies to.</para>
        /// </summary>
        public static bool MayChooseTheFace(
            HeroId heroId,
            IReadOnlyList<CardInstanceId> activeEffectSourceCardInstanceIds,
            IReadOnlyList<CardInstance> cards,
            IReadOnlyList<CardDefinition> definitions)
        {
            if (!heroId.IsDefined || activeEffectSourceCardInstanceIds == null ||
                activeEffectSourceCardInstanceIds.Count == 0 || cards == null || definitions == null)
            {
                return false;
            }

            foreach (var cardInstanceId in activeEffectSourceCardInstanceIds)
            {
                var card = cards.FirstOrDefault(value =>
                    value != null && value.Id.Equals(cardInstanceId));
                if (card == null || !card.OwnerId.Equals(heroId))
                {
                    continue;
                }

                var definition = definitions.FirstOrDefault(value =>
                    value != null && value.Reference.Equals(card.DefinitionReference));
                if (definition != null &&
                    definition.PrintedTextSelection == PrintedTextSelection.FreesTheCoinChoice)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

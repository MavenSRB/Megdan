using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.71b's component declarations for The Ultimate Trick: NONE, on either side.
    ///
    /// <para><b>Stated rather than omitted</b>, on the terms every hero's component partial before
    /// his states them - 8.71e's co-moves declare the same empty pair for the same kind of reason.
    /// The block that owns a clause says what it withheld from production registration, and a block
    /// that withheld nothing says that.</para>
    ///
    /// <para><b>Why there is nothing.</b> The Ultimate Trick composes no effect body (see
    /// <see cref="HanuEffectContent"/>), so there is no capability to write, to defer or to
    /// register: p.16's permanent ability is a standing declaration, and the rule that reads it -
    /// <c>DelegatedActionQuery</c> - is engine content reached through the seat projection rather
    /// than through the effect frame. <see cref="MatchContentCatalogs"/> therefore still names no
    /// Hanu content at all, which is milestone 7.9's gate, and this block adds no new handler TYPE
    /// for 8.71h to owe a showcase consumer.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>Nothing: this face declares no capability to register.</summary>
        public static IEnumerable<IEffectCapabilityHandler> UltimateRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>Nothing: this face declares no capability to defer either.</summary>
        public static IEnumerable<IEffectCapabilityHandler> UltimateDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}

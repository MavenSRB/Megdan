using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's contracts, as ONE set - milestone 8.69f.
    ///
    /// <para><b>What this file adds to the five partials beside it.</b> 8.69a declared the predicate
    /// his conditional sentences read, 8.69b the question his three split faces ask of the Attack that
    /// is running, and 8.69d the union selection his three swaps choose over; 8.69c and 8.69e declared
    /// nothing at all, which was each of those blocks' own headline result. Each carried a block-scoped
    /// pair of lists, because four partials of one class were being written at once. What a DEALING
    /// needs is one list per question - which contracts does this hero run on, and which of them does
    /// this build withhold - and those are the two aggregates below. The block lists stay where they
    /// are and remain the homes of their own declarations.</para>
    ///
    /// <para><b>He puts nothing on a board and holds no triggered effect.</b> There is no
    /// <c>Tokens()</c> here and no <c>TriggeredEffects</c>, where Min's equivalent file has both: no
    /// face of his names a token or a marker (his shard records that outright), and his Ultimate is a
    /// permanent ability whose whole content is a standing rule declaration rather than a body a
    /// <c>TriggeredEffectCatalog</c> must hold. His set is positional from end to end.</para>
    ///
    /// <para><b>Nothing here is registered, and that is the point rather than an oversight.</b>
    /// <see cref="MatchContentCatalogs"/> names no Brynn content until 8.69g deals him, so no
    /// configured match can start one of his capabilities - milestone 7.9's gate, on the terms every
    /// block of his recorded: the registered list is serialized and pinned, so a contract registered
    /// before a dealt card can start it moves pinned bytes while no content moved. So
    /// <see cref="DeferredHandlers"/> is the whole list and <see cref="RegisteredHandlers"/> is empty -
    /// the arrangement <c>MinComponentContent</c> carried at 8.61i and its dealing then reversed.</para>
    ///
    /// <para><b>All three deferred contracts are new handler TYPES</b>, so 8.69g owes each a showcase
    /// consumer as well as a registration - the second half of milestone 7.9's gate. A FOURTH
    /// declaration, 8.69a's <c>brynn-hero-adjacent-to-obstacles@1</c>, is deliberately NOT here: no
    /// composed sentence of his reaches it, because 8.69b measured the split faces' gate to be a
    /// question about the targeting rather than about a hero and minted its own. A capability nothing
    /// can start is a registration this milestone DROPPED rather than one a dealing makes reachable by
    /// nothing, and the declaration is gone from the tree.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// Everything his set declares, registered or not, for the offline content fixtures that
        /// compose catalogs of their own - the shape <c>MinComponentContent.Handlers</c> keeps.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// Nothing, until 8.69g deals him. The member exists so a "registered or deferred" sweep keeps
        /// its shape and so the fixtures asserting the gate ask a question rather than failing to
        /// compile.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            ObstacleRegisteredHandlers()
                .Concat(SplitAttackRegisteredHandlers())
                .Concat(GuidesRegisteredHandlers())
                .Concat(SwapRegisteredHandlers())
                .Concat(TrapAndGritRegisteredHandlers());

        /// <summary>
        /// His THREE withheld contracts: 8.69a's <c>brynn-enemy-hero-in-radius-adjacent-to-obstacles@1</c>
        /// (Mountain Guide, Expedition Leader, True Grit, Die Hard), 8.69b's
        /// <c>brynn-attack-target-was-adjacent-to-obstacles@1</c> (Split Attack, Split Throw, Peak
        /// Precision) and 8.69d's <c>brynn-tracking-swap-subject@1</c> (Tread Lightly, Cover Tracks,
        /// Hide Traces). 8.69c and 8.69e contribute empty lists, which is their own result rather than
        /// an omission, and the empty concats stay so a block that later defers something finds the
        /// shape already here.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            ObstacleDeferredHandlers()
                .Concat(SplitAttackDeferredHandlers())
                .Concat(GuidesDeferredHandlers())
                .Concat(SwapDeferredHandlers())
                .Concat(TrapAndGritDeferredHandlers());
    }
}

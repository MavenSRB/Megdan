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
    /// <para><b>All three are REGISTERED since milestone 8.69g dealt him, and the order of those two
    /// facts is the whole of milestone 7.9's gate.</b> <see cref="MatchContentCatalogs"/> named no
    /// Brynn content until his eighteen faces were cards a configured match deals, because the
    /// registered list is serialized and pinned: a contract registered before a dealt card can start it
    /// moves pinned bytes while no content moved. 8.69g flipped both halves in ONE change - the
    /// registration and the dealing - which is Silverarrow's 8.35z46 law. So
    /// <see cref="RegisteredHandlers"/> is now the whole list and <see cref="DeferredHandlers"/> is
    /// empty; the empty concats stay, because an empty deferral list is what a discharged gate looks
    /// like and the next block to withhold something of his finds the shape already here.</para>
    ///
    /// <para><b>All three are new handler TYPES</b>, so 8.69g gave each a showcase consumer as well as
    /// a registration - the second half of milestone 7.9's gate: the Red Surveyor for the radius
    /// presence predicate, the Red Pathkeeper for the union swap selection, and the Red Quarryman's
    /// "After the attack" rider for the question the split faces ask of the Attack that is running,
    /// which needs an enclosing Attack and therefore cannot be a Skill. A FOURTH declaration, 8.69a's
    /// <c>brynn-hero-adjacent-to-obstacles@1</c>, is deliberately NOT here: no composed sentence of his
    /// reaches it, because 8.69b measured the split faces' gate to be a question about the targeting
    /// rather than about a hero and minted its own. A capability nothing can start is a registration
    /// 8.69f DROPPED rather than one a dealing makes reachable by nothing.</para>
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
        /// His THREE contracts, all registered since 8.69g dealt him: 8.69a's
        /// <c>brynn-enemy-hero-in-radius-adjacent-to-obstacles@1</c> (Mountain Guide, Expedition
        /// Leader, True Grit, Die Hard), 8.69b's <c>brynn-attack-target-was-adjacent-to-obstacles@1</c>
        /// (Split Attack, Split Throw, Peak Precision) and 8.69d's <c>brynn-tracking-swap-subject@1</c>
        /// (Tread Lightly, Cover Tracks, Hide Traces). 8.69c and 8.69e contribute empty lists, which
        /// was each of those blocks' own headline result.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            ObstacleRegisteredHandlers()
                .Concat(SplitAttackRegisteredHandlers())
                .Concat(GuidesRegisteredHandlers())
                .Concat(SwapRegisteredHandlers())
                .Concat(TrapAndGritRegisteredHandlers());

        /// <summary>
        /// EMPTY since 8.69g dealt him, and stated rather than deleted: every one of the three moved to
        /// <see cref="RegisteredHandlers"/> in the same change that made his eighteen faces dealable.
        /// The five block concats stay so a later block of his that withholds something finds the shape
        /// already here, and so the sweeps reading "registered or deferred" keep asking one question.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            ObstacleDeferredHandlers()
                .Concat(SplitAttackDeferredHandlers())
                .Concat(GuidesDeferredHandlers())
                .Concat(SwapDeferredHandlers())
                .Concat(TrapAndGritDeferredHandlers());
    }
}

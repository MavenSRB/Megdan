using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's components and contracts, as ONE set - milestone 8.61i.
    ///
    /// <para><b>What this file adds to the four partials beside it.</b> 8.61a declared her Smoke bomb,
    /// 8.61d her two mine kinds and their facedown face, 8.61f her Grenade and 8.61h the two Active
    /// effects her doubling faces create, each with a block-scoped list of its own because eight
    /// partials of one class were being written at once. What a DEALING needs is one list per
    /// question - which tokens does this hero put on a board, which contracts does she run on, which
    /// triggered effects must a catalog hold - and those are the three aggregates below. The block
    /// lists stay where they are and remain the homes of their own declarations.</para>
    ///
    /// <para><b>Nothing here is registered, and these aggregates are what 8.61j moves.</b>
    /// <see cref="MatchContentCatalogs"/> names no Min content, so no configured match can put one of
    /// her pieces on a board and nothing reachable can start one of her capabilities - milestone 7.9's
    /// gate, on the terms every block of hers recorded: the registered list is serialized and pinned,
    /// so a contract registered before a dealt card can start it moves pinned bytes while no content
    /// moved. <see cref="RegisteredHandlers"/> is empty and <see cref="DeferredHandlers"/> is the whole
    /// debt, which is the arrangement <c>BainComponentContent</c> left behind after its own dealing
    /// moved four handlers across.</para>
    ///
    /// <para><b>Two contracts of hers are deliberately NOT in this file's list</b>, because they are
    /// shared vocabulary rather than her components: <c>borrow-after-attack-text@1</c> waits in
    /// <c>SyntheticBorrowedCardTextCapabilities.DeferredHandlers</c> and
    /// <c>take-deck-card-as-item@1</c> in <c>SyntheticRosterCapabilities.DeferredHandlers</c>, each
    /// beside the family that owns its handler type. Both are new handler TYPES, so 8.61j owes each a
    /// showcase consumer as well as a registration - which is the second half of milestone 7.9's gate,
    /// and the half that cannot be discharged by a dealing alone.</para>
    /// </summary>
    public static partial class MinComponentContent
    {
        /// <summary>
        /// Every token definition her cards can put on a board: the Smoke bomb, the blast, the dud and
        /// the facedown face every seat but hers is handed in place of a real one, and the Grenade.
        ///
        /// <para>Five definitions for three printed token KINDS, and the fourth is the reason: a
        /// facedown mine is a real registered definition rather than an absence, because a client loads
        /// a token's definition by id from the catalog and the substituted value has to be something
        /// the catalog knows.</para>
        /// </summary>
        public static IEnumerable<TokenDefinition> Tokens() =>
            SmokeBombTokens().Concat(MineTokens()).Concat(GrenadeTokens());

        /// <summary>
        /// Everything her set declares, registered or not, for the offline content fixtures that
        /// compose catalogs of their own - the shape <c>BainComponentContent.Handlers</c> keeps.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// Nothing, until milestone 8.61j deals her. The member exists so that the fixtures asserting
        /// the deferral from both sides ask a question rather than failing to compile, and so the
        /// "registered or deferred" shape survives the dealing that empties the list below.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            SmokeBombRegisteredHandlers().Concat(DisciplineRegisteredHandlers());

        /// <summary>
        /// Every contract of hers this build withholds: 8.61a's six Smoke bomb operations, 8.61d's two
        /// mine placements and one reclaim, 8.61f's grenade presence, space, place, reclaim, two
        /// anchored hero selections and two end-of-turn Active effects, and 8.61h's two doubling Active
        /// effects. The two contracts that live in a shared family's own deferred list are named in
        /// this type's remarks.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            SmokeBombDeferredHandlers()
                .Concat(MineDeferredHandlers())
                .Concat(GrenadeDeferredHandlers())
                .Concat(DisciplineDeferredHandlers());

        /// <summary>
        /// Every effect definition a <c>TriggeredEffectCatalog</c> must hold for her dealt faces to
        /// resolve: the blast's four trap consequences, one per number of blasts a single crossing can
        /// remove (8.61e), both Grenades' end-of-turn bodies (8.61f) and Flurry of Blows' repeat
        /// (8.61h).
        ///
        /// <para>They are triggered effects rather than card text for one reason each, and the reasons
        /// are different: a mine's price is paid on a route somebody else walks on a later turn, a
        /// Grenade's sentence runs at the end of the turn that placed it, and an Ultimate's ability
        /// fires after an action rather than as one.</para>
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects => LazyTriggered.Value;

        // Lazy on the convention milestone 8.38d adopted and this milestone re-earned: an eager static
        // field here would run inside this class's own initializer while reading another content
        // class's statics, which is how a static-initialization cycle starts.
        private static readonly System.Lazy<IReadOnlyList<EffectDefinition>> LazyTriggered =
            new System.Lazy<IReadOnlyList<EffectDefinition>>(() => System.Array.AsReadOnly(
                MinEffectContent.MineTrapEffects()
                    .Concat(MinEffectContent.GrenadeTriggeredEffects)
                    .Concat(new[]
                    {
                        MinEffectContent.FlurryOfBlowsTriggeredEffect(MinCardContent.ContentRevision)
                    })
                    .ToArray()));
    }
}

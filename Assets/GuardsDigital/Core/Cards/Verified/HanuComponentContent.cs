using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's contracts, as ONE set - milestone 8.71g.
    ///
    /// <para><b>What this file adds to the six partials beside it.</b> 8.71a declared the Initiative
    /// rewrite Hurry up! performs, 8.71d the partner lookup his journeys' end-of-turn bodies re-derive
    /// plus one Active-effect creation per journey face, and 8.71f the friendly PAIR his Greens
    /// exchange; 8.71b, 8.71c and 8.71e declared nothing at all, which was each of those blocks' own
    /// headline result. Each carried a block-scoped pair of lists, because five partials of one class
    /// were being written at once and a member declared twice is a compile error rather than a merge.
    /// What a DEALING needs is one list per question - which contracts does this hero run on, which of
    /// them does this build withhold, and which triggered bodies must a catalog hold - and those are the
    /// three aggregates below. The block lists stay where they are and remain the homes of their own
    /// declarations.</para>
    ///
    /// <para><b>He puts nothing on a board and gives no marker.</b> There is no <c>Tokens()</c> here,
    /// where Min's equivalent file has one: no face of his names a token, a marker or a component at all
    /// (his shard records that outright - "He places nothing and gives no marker"). What his set does is
    /// reposition units, rewrite one Initiative and, at Tier III, take another hero's next action.</para>
    ///
    /// <para><b>Nothing is registered, and that is milestone 7.9's gate rather than an oversight.</b>
    /// <see cref="MatchContentCatalogs"/> names no Hanu content, so <see cref="RegisteredHandlers"/> is
    /// EMPTY and <see cref="DeferredHandlers"/> is the whole list: the registered list is serialized and
    /// pinned, so a contract registered before a dealt card can start it moves pinned bytes while no
    /// content moved. 8.71h is the block that flips both halves in one change - the registration and
    /// the dealing - which is Silverarrow's 8.35z46 law, and the six references are the exact debt
    /// <c>HanuPrintedSetTests</c> states from both sides.</para>
    ///
    /// <para><b>THREE of the six are new handler TYPES</b>, so 8.71h owes each a showcase consumer as
    /// well as a registration - the second half of milestone 7.9's gate:
    /// <c>hanu-hurry-up-initiative@1</c>, <c>hanu-journey-partner@1</c> and
    /// <c>hanu-friendly-unit-pair@1</c>. The other three are
    /// <see cref="GuardsDigital.Core.Effects.ActiveEffectCreationHandler"/> registrations over the three
    /// definitions in <see cref="HanuActiveEffectContent"/> - one per journey face, because that handler
    /// is one registration per definition rather than a type of its own.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// Everything his set declares, registered or not, for the offline content fixtures that compose
        /// catalogs of their own - the shape <c>BrynnComponentContent.Handlers</c> keeps.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// NOTHING, until 8.71h deals him. Stated as the six block concats rather than as an empty
        /// array, so the dealing moves declarations between two lists whose shape already exists and no
        /// sweep asking "registered or deferred" has to learn a new member name.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            HurryUpRegisteredHandlers()
                .Concat(UltimateRegisteredHandlers())
                .Concat(RedRegisteredHandlers())
                .Concat(JourneyRegisteredHandlers())
                .Concat(CoMoveRegisteredHandlers())
                .Concat(GreenRegisteredHandlers());

        /// <summary>
        /// His SIX contracts, every one withheld from production registration: 8.71a's
        /// <c>hanu-hurry-up-initiative@1</c> (Hurry up!), 8.71d's <c>hanu-journey-partner@1</c> and its
        /// three Active-effect creations <c>hanu-unexpected-journey-effect@1</c>,
        /// <c>hanu-there-and-back-again-effect@1</c> and <c>hanu-safe-travels-effect@1</c> (the three
        /// Blue journeys), and 8.71f's <c>hanu-friendly-unit-pair@1</c> (Monkey Trick, Monkey Twist and
        /// Monkey Business). 8.71b, 8.71c and 8.71e contribute empty lists, which was each of those
        /// blocks' own headline result.
        ///
        /// <para><see cref="HanuActiveEffectContent"/>'s three handlers are NOT concatenated here
        /// separately: <c>JourneyDeferredHandlers</c> already carries them, because they belong to the
        /// clause that creates them, and a second concat would hand the same reference in twice -
        /// milestone 8.40j's BA-18, "Capability references must be unique", which is what threw at
        /// Bain's dealing.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            HurryUpDeferredHandlers()
                .Concat(UltimateDeferredHandlers())
                .Concat(RedDeferredHandlers())
                .Concat(JourneyDeferredHandlers())
                .Concat(CoMoveDeferredHandlers())
                .Concat(GreenDeferredHandlers());

        /// <summary>
        /// Every effect definition a <c>TriggeredEffectCatalog</c> must hold for his dealt faces to
        /// resolve: the three end-of-turn bodies his Blue journeys arm, one per face.
        ///
        /// <para>They are triggered effects rather than card text for the reason p.17 gives: "End of
        /// turn: Swap with that hero regardless of radius and immunity" runs at the end of the turn that
        /// played the card rather than as part of the action, so the body is held by an Active effect
        /// and fired at <c>TimingCheckpoint.TurnEnded</c>. His Ultimate names no triggered effect at all
        /// - <c>The Ultimate Trick</c> is a standing declaration a rule query derives, which is why
        /// there is one entry per journey here and not a fourth.</para>
        /// </summary>
        public static IReadOnlyList<EffectDefinition> TriggeredEffects => LazyTriggered.Value;

        // Lazy on the convention milestone 8.38d adopted: an eager static field here would run inside
        // this class's own initializer while reading another content class's statics, which is how a
        // static-initialization cycle starts.
        private static readonly System.Lazy<IReadOnlyList<EffectDefinition>> LazyTriggered =
            new System.Lazy<IReadOnlyList<EffectDefinition>>(() => System.Array.AsReadOnly(
                HanuEffectContent.JourneyTriggeredEffects().ToArray()));
    }
}

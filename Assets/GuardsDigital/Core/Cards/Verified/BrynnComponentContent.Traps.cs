using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.69e's half of Brynn the Seeker's contracts, and the whole of it is a statement that
    /// there are none.
    ///
    /// <para><b>Five printed faces and not one new capability, which is the block's own result rather
    /// than a gap.</b> Every clause the three traps and the two grit faces print is already a contract
    /// this build carries: p.10's modal is a node KIND, "an enemy hero adjacent to you" is
    /// <c>choose-target@2</c>, "an enemy hero in radius who is adjacent to 3 or more obstacles" is
    /// <c>synthetic-radius-hero-target@1</c> with milestone 8.69a's optional Boolean, "discards a card"
    /// is <c>synthetic-discard-chosen-card</c>'s two live revisions, "or is defeated" is
    /// <c>synthetic-is-flagged@1</c> over <c>synthetic-defeat-hero@1</c>, "You may retrieve a discarded
    /// attack card" is <c>retrieve-card@1</c> with this milestone's optional <c>attackCardsOnly</c>
    /// filter, and "move up to N spaces" is <c>actor-space@1</c> into <c>synthetic-direct-move@3</c>.
    /// The one clause with no shared owner - "If an enemy hero in radius is adjacent to 3 or more
    /// obstacles" - was minted by 8.69a as
    /// <see cref="EnemyHeroInRadiusAdjacentToObstacles"/> and is on THAT block's deferral list, where
    /// it stays.</para>
    ///
    /// <para><b>The empty list is kept rather than omitted</b>, for the reason
    /// <c>SyntheticTargetingCapabilities.DeferredHandlers</c> is kept empty across the milestones that
    /// empty it: a "registered or deferred" sweep keeps its shape, the fixture asserting the gate asks
    /// a question rather than failing to compile, and the next block of his that defers something finds
    /// the name already here. Nothing of this block's reaches
    /// <see cref="MatchContentCatalogs"/>; 8.69g is the block that deals him.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// The placeholder revision the five faces milestone 8.69e composes carry, replaced by 8.69f
        /// with the one real string his whole set is stated under.
        ///
        /// <para>Deliberately malformed, on the project's standing rule that a placeholder must not be
        /// a value that could be mistaken for a real one - and its own, rather than 8.69a's
        /// <see cref="PrepRevision"/>, so the four blocks of the parallel wave can be told apart in a
        /// composed list before 8.69f folds them into one.</para>
        /// </summary>
        public const string TrapPrepRevision = "brynn-8-69e-prep";

        /// <summary>
        /// Nothing, and see the type's own remarks for why that is the milestone's answer rather than
        /// an omission. The member sits beside <see cref="ObstacleDeferredHandlers"/> rather than
        /// inside it, because a partial class cannot declare one member twice and the two lists belong
        /// to two blocks.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> TrapAndGritDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Nothing, for the same reason 8.69a registers nothing: milestone 7.9's gate holds until a
        /// dealt card of his can start a contract.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> TrapAndGritRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}

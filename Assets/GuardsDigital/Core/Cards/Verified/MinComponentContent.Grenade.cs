using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61f. Min the Dragonmonk's Grenade token - the third of her three printed token
    /// kinds - and the capabilities her two Blue "Death Grenade" faces reach it through.
    ///
    /// <para><b>What the source says.</b> Death Grenade prints "Place a [Grenade] Grenade token into a
    /// space in radius. End of turn: Up to 1 enemy hero adjacent to that token discards a card, or is
    /// defeated. Remove the Grenade token."; Holy Death Grenade prints the same three sentences with
    /// "Up to 2 enemy heroes ... discard a card, or are defeated". See
    /// <c>Docs/AI/Rules/HERO_MIN.md</c>, MI-01 and MI-Q4.</para>
    ///
    /// <para><b>The supply is ONE, under MI-Q1.</b> The count is the user's to state and has been
    /// asked; until it is answered the working assumption is one grenade, and two printed facts point
    /// the same way. Both faces say "<b>a</b> [Grenade] Grenade token" in the singular and then
    /// "<b>the</b> Grenade token" with a definite article, and neither face ever puts two out - the
    /// second sentence removes the one the first placed, in the same turn. One is also what
    /// <see cref="SyntheticOwnedTokenCapabilities"/> requires of a named-token fact, which is the
    /// contract "that token" is read through; a larger supply would make "that token" a selection
    /// rather than a fact and is the one thing that changes here if the user rules otherwise.</para>
    ///
    /// <para><b>The removal condition is p.19's baseline and the card's own sentence does not change
    /// it.</b> "Remove the Grenade token" happens at the end of the turn it was placed, as a step of
    /// the effect the card scheduled - it is work the card performs, not a lifetime the component
    /// carries - so the definition still declares <see cref="TokenRemovalCondition.EndOfRoundCleanup"/>
    /// and p.19's cleanup is the fallback for the turn that never reaches its own end (the placing
    /// hero defeated, her cards retrieved: p.17's two cancellations of the Active effect that carries
    /// the sentence). Widget's Pyro is the contrast and the reason the enum has two members at all -
    /// Dragon Bond prints an explicit override and this does not.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Every capability below is deferred until 8.61j deals
    /// her, for milestone 7.9's reason: the registered list is serialized and pinned, so a contract
    /// registered before a dealt card can start it moves pinned bytes while no content moved. Both
    /// handler TYPES these instances run on - <c>SyntheticOwnedTokenCapabilities</c>'s fact handler,
    /// <c>SyntheticTokenCapabilities</c>'s place and reclaim, and
    /// <see cref="ActiveEffectCreationHandler"/> - are already registered and showcased by earlier
    /// milestones, so none of these instances owes the dealing a showcase consumer of its own.</para>
    /// </summary>
    public static partial class MinComponentContent
    {
        /// <summary>MI-Q1's working count: the one physical grenade in her box.</summary>
        public const int GrenadeSupplyCount = 1;

        /// <summary>
        /// The Grenade, carrying no property at all. It modifies nothing while it stands - unlike
        /// Tali's Ice and her Totems, whose printed sentences are about the pieces - and what it does
        /// on the board it does by being a game piece: p.19 classes a token as an obstacle and p.18
        /// admits no second obstacle in a space, so the grenade blocks its own space for as long as the
        /// turn lasts without this definition saying so.
        /// </summary>
        public static TokenDefinition GrenadeDefinition { get; } = new TokenDefinition(
            new TokenDefinitionId("min-grenade"),
            1,
            "Grenade",
            GrenadeSupplyCount,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.EndOfRoundCleanup);

        /// <summary>Whether her one grenade stands, which is the branch between placing and relocating.</summary>
        public static EffectCapabilityReference GrenadePresentV1 => GrenadeReference("min-grenade-present");

        /// <summary>
        /// Where her one grenade stands. "That token" in both faces' second sentence is read through
        /// this: with a supply of one, the actor-owned grenade on the board IS the instance the first
        /// sentence placed, and a grenade that is gone by the end of the turn publishes nothing - which
        /// is the mandatory step p.10 stops at, leaving the rest of the sentence undone rather than
        /// failing.
        /// </summary>
        public static EffectCapabilityReference GrenadeSpaceV1 => GrenadeReference("min-grenade-space");

        /// <summary>"Place a [Grenade] Grenade token into a space in radius" from the supply.</summary>
        public static EffectCapabilityReference PlaceGrenadeV1 => GrenadeReference("min-place-grenade");

        /// <summary>
        /// p.18's short-supply step for the grenade pile.
        ///
        /// <para><b>No composed face reaches it, and that is recorded rather than hidden.</b> With one
        /// grenade and the presence branch both faces open with, a placement that finds one standing
        /// RELOCATES it (MI-Q1) instead of asking the supply for a second - so the pile is never short.
        /// It is declared here because the deliverable list asks for it and because a later face or a
        /// larger supply under MI-Q1 would reach it immediately; 8.61i and 8.61j should drop it if the
        /// answered count still leaves no printed sentence that can run out.</para>
        /// </summary>
        public static EffectCapabilityReference ReclaimGrenadeV1 => GrenadeReference("min-reclaim-grenade");

        /// <summary>Her one token definition this block declares, for a fixture composing a catalog.</summary>
        public static IEnumerable<TokenDefinition> GrenadeTokens() => new[] { GrenadeDefinition };

        /// <summary>
        /// Every capability the two Grenade faces name, and every one of them is deferred - see the
        /// type's own remarks. The Active-effect creations live in <see cref="MinEffectContent"/>
        /// because the definitions they create are composed beside the triggered bodies they schedule,
        /// and they are concatenated here so this list is THE deferred list for the block: a fixture
        /// that asserts the absence from production registration asks one question, not two.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> GrenadeDeferredHandlers() =>
            new[]
            {
                SyntheticOwnedTokenCapabilities.PresenceHandler(GrenadePresentV1, GrenadeDefinition),
                SyntheticOwnedTokenCapabilities.SpaceHandler(GrenadeSpaceV1, GrenadeDefinition),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceGrenadeV1, GrenadeDefinition, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimGrenadeV1, GrenadeDefinition),
                SyntheticAnchoredTargetCapabilities.Handler(
                    MinEffectContent.GrenadeBlastTargetV1,
                    RosterUnitKind.Hero,
                    SourceTeamRelation.OpposingSource,
                    AnchorTargetShape.Adjacent,
                    withinPrintedRange: false),
                SyntheticAnchoredTargetCapabilities.Handler(
                    MinEffectContent.GrenadeSecondBlastTargetV1,
                    RosterUnitKind.Hero,
                    SourceTeamRelation.OpposingSource,
                    AnchorTargetShape.Adjacent,
                    withinPrintedRange: false,
                    excludesNamedHero: true)
            }
            .Concat(MinEffectContent.GrenadeActiveEffectHandlers());

        private static EffectCapabilityReference GrenadeReference(string id) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), 1);
    }
}

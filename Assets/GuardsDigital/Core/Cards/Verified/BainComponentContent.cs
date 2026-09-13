using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The bounty marker, and the capability that gives it. Milestone 8.40a, and the second
    /// hero-owned rules component in this project after Tigerclaw's Poison.
    ///
    /// <para><b>What the source says.</b> Dead or Alive prints "You may give an enemy hero in radius a
    /// [bounty] bounty marker; a hero with a bounty marker spends 1 additional [life] life counter when
    /// defeated", and Side Quest prints the same rule as a mandatory give gated on Bain's own adjacency.
    /// The extraction independently contains the matching readable <c>BountyMarker</c> component - a
    /// "WANTED" poster nailed to a board inside a riveted steel ring - sitting immediately beside
    /// Tigerclaw's <c>PoisonMarker</c>. The values are the cards'; the component is the physical piece.
    /// See <c>Docs/AI/Rules/HERO_BAIN.md</c>, BA-02, BA-13, BA-14 and BA-17.</para>
    ///
    /// <para><b>Why the rule is on the marker rather than on the card.</b> The sentence is printed in
    /// the present tense about the holder - "a hero <em>with</em> a bounty marker spends" - so it is
    /// true while the marker is held and stops when it is returned, which is p.8's end of round and
    /// p.13's holder Defeat. That is a marker property, exactly as Tigerclaw's three value modifiers
    /// are, and <c>RuleModificationQuery.FromHeldMarkers</c> makes it affect
    /// <c>RuleObject.ForHero(marker.HolderHeroId)</c> without this file saying so.</para>
    ///
    /// <para><b>Deferred, not registered, and both halves are deferred together.</b> Neither
    /// <see cref="Markers"/> nor <see cref="DeferredHandlers"/> is reachable from
    /// <c>SyntheticComponentDefinitions.CreateCatalog</c> or <c>MatchContentCatalogs</c>: a match
    /// registers one list of what it can put on a dashboard and one list of what it can run, and a
    /// snapshot pins both, so registering either before a dealt card can start it moves pinned bytes
    /// and fails milestone 7.9's reachability gate. Milestone 8.40j deals him and moves both across in
    /// the same change; <c>BainMarkerDefeatCostTests</c> asserts the deferral from both sides so that
    /// dealing cannot register one half and forget the other. The handler TYPE is Tigerclaw's, already
    /// registered and already showcased, so this instance owes no showcase consumer.</para>
    /// </summary>
    public static class BainComponentContent
    {
        /// <summary>
        /// The supply, and it is <b>one</b> - BA-D1, recorded for the user to overrule.
        ///
        /// <para>Two printed facts point the same way. His Ultimate says "the bounty marker" with a
        /// definite article, and all five of his Green faces ask whether "<b>a</b> hero in play has a
        /// bounty marker" in the singular before Close Call says "that hero gives <b>the</b> marker to
        /// you". Tigerclaw's six is the alternative and is rejected here for a measured reason rather
        /// than a stylistic one: his two strengths are two definitions of one physical stack and every
        /// hero on the board can wear one, while nothing in Bain's set ever puts two out.</para>
        ///
        /// <para>The consequence is milestone 8.40b's, not this one's: with a supply of one,
        /// <c>MarkerGivingHandler</c> reports impossible when the marker is already held, and the
        /// recorded reading is that the give MOVES it. This milestone implements the count; that one
        /// implements the move.</para>
        /// </summary>
        public const int BountySupplyCount = 1;

        public static MarkerDefinitionId Bounty { get; } = new MarkerDefinitionId("bounty");

        /// <summary>
        /// The marker Dead or Alive and Side Quest give: one property, carrying the additional p.13
        /// Life cost its holder pays when defeated, and nothing else. It modifies no printed value
        /// channel at all, which is the whole difference between it and Tigerclaw's.
        /// </summary>
        public static MarkerDefinition BountyDefinition { get; } =
            new MarkerDefinition(
                Bounty,
                1,
                "Bounty",
                BountySupplyCount,
                // p.8 returns every marker at end of round and p.13 returns a defeated hero's. The two
                // are one condition in this vocabulary (BA-02) and no face of his prints an earlier one.
                MarkerReturnCondition.GeneralCleanup,
                new[]
                {
                    new MarkerPropertyDefinition(
                        "additional-defeat-life-cost",
                        "additional-defeat-life-cost-when-defeated",
                        "while-held",
                        RuleModification.ForAdditionalDefeatLifeCostWhenDefeated())
                });

        public static EffectCapabilityId GiveBounty { get; } =
            new EffectCapabilityId("bain-give-bounty-marker");

        public static EffectCapabilityReference GiveBountyV1 =>
            new EffectCapabilityReference(GiveBounty, 1);

        /// <summary>
        /// His one marker definition, for the catalog milestone 8.40j registers it into. It is NOT in
        /// the registered component catalog yet - see the type's own remarks.
        /// </summary>
        public static IEnumerable<MarkerDefinition> Markers() => new[] { BountyDefinition };

        /// <summary>
        /// Everything this file declares, registered or not, for the offline content fixtures that
        /// compose catalogs of their own.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// Deliberately empty until milestone 8.40j deals him, for the reason
        /// <c>SyntheticConditionCapabilities</c> states: the registered capability list is serialized
        /// into every snapshot, so registering a capability before its hero is dealt moves pinned bytes
        /// and reads at the far end like a serialization defect rather than like content arriving.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The give itself, waiting for the dealing. One capability per marker, exactly as Tigerclaw's
        /// two are and for the same reason: the definition is bound to the handler rather than named on
        /// the wire, so a payload can name a node but never a different marker.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new SyntheticComponentCapabilities.MarkerGivingHandler(
                    GiveBountyV1, BountyDefinition)
            };
    }
}

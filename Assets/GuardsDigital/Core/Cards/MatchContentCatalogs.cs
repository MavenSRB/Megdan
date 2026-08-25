using System;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Cards.Verified;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The trusted capability and component catalogs one match runs on, travelling together because
    /// they are always composed, persisted, restored and rebound together.
    ///
    /// <para><b>Why this exists</b> - milestone 8.3, closing the 7.9a review's F-CARD-05. Eleven
    /// production sites used to mint a synthetic catalog when they were handed none: five inside the
    /// card-action dispatcher's constructors and restore arms, two inside the snapshot serializer's
    /// write and read paths, and the component-catalog twins beside them. Each was an independent,
    /// silent decision, and each would have to be found and changed by hand the first time a verified
    /// capability existed. The serializer's own comment claimed "the configured session always supplies
    /// the real ones", and it did not: <c>MatchAggregate</c> called both the capture and the restore
    /// with no catalogs at all, so the configured match's snapshots were written and rebound against a
    /// default nobody had chosen. The comment is corrected in the same edit that removes the
    /// defaults.</para>
    ///
    /// <para><b>The rule that replaces them.</b> Every composition site takes this as a required
    /// argument. There is exactly one place in production that builds one - <see cref="ForThisBuild"/>
    /// below - and <c>EffectSeamConformanceTests.NoProductionCompositionSite_NamesACapabilityCatalogItWasNotGiven</c>
    /// fails the build if any other production type so much as names a catalog factory. Asking this
    /// type what the build registers is a different act from a dispatcher quietly substituting content:
    /// one is the build declaring what it can do, the other is a consumer inventing what it was not
    /// given.</para>
    ///
    /// <para><b>Catalog identity, decided here.</b> A catalog carries no identity of its own and gets
    /// none: the snapshot records the registration list - every capability's id and revision, and every
    /// component definition's - and rebinds against the build's own catalogs on restore. A catalog name
    /// on the wire would be a second, coarser identity that could agree while the contents differed,
    /// which is precisely the silent-reinterpretation failure <c>RISK-COMPAT-001</c> exists to prevent;
    /// the registration list cannot, because it is the contents. The decision is recorded beside that
    /// risk row in <c>Docs/AI/TECHNICAL_RISKS.md</c>.</para>
    /// </summary>
    public sealed class MatchContentCatalogs
    {
        public MatchContentCatalogs(
            EffectCapabilityCatalog capabilities,
            ComponentDefinitionCatalog components,
            TriggeredEffectCatalog triggeredEffects = null)
        {
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Components = components ?? throw new ArgumentNullException(nameof(components));
            TriggeredEffects = triggeredEffects ?? TriggeredEffectCatalog.Empty;
        }

        /// <summary>The registered capability handlers a Skill's effect frame may reach.</summary>
        public EffectCapabilityCatalog Capabilities { get; }

        /// <summary>The registered token and marker definitions a component operation may reach.</summary>
        public ComponentDefinitionCatalog Components { get; }

        public TriggeredEffectCatalog TriggeredEffects { get; }

        /// <summary>
        /// What this build registers - the single declared origin of both catalogs.
        ///
        /// It is synthetic content today because that is what the build has: milestone 6.2a's bounded
        /// capability set and 6.2's component definitions, which the configured match genuinely runs on.
        /// It is not a stand-in for missing content and not a default substituted for an absent
        /// argument; it is the answer to "what can this build do", and it is the one line that changes
        /// as each 8.5-8.13 slice registers a verified capability beside them.
        ///
        /// A fresh bundle per call, matching what the eleven sites this replaced already did: the
        /// catalogs are immutable and their handlers are frame-in/frame-out, so nothing is shared and
        /// nothing is saved by caching one.
        /// </summary>
        public static MatchContentCatalogs ForThisBuild() =>
            new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(new[]
                {
                    WaspEffectContent.HighVoltageTriggeredEffect(WaspCardContent.ContentRevision),
                    // Milestone 8.19a. The effect a `before-action-trigger@1` source runs. It joins the
                    // one catalog rather than forming a second, for the reason 8.9's verified effects
                    // did: a match registers one set and the snapshot compares one.
                    GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                        .BeforeActionTriggeredEffect.Definition,
                    // Milestone 8.19c1. The effect milestone 8.19c1's two new trigger windows run -
                    // p.8's moment before the Minion Battle, and the moment a Push applies. One
                    // definition for both, because what each of them is proving is the window rather
                    // than the text, and the text is the same two questions either way.
                    GuardsDigital.Core.Effects.Synthetic.SyntheticPausingSourceCapabilities
                        .PausingTriggeredEffect.Definition,
                    // Milestone 8.20. What Arien's Living Tsunami runs before an Attack action. It
                    // joins this catalog rather than forming a second, for the reason High Voltage's
                    // does: a match registers one set and the snapshot compares one.
                    ArienEffectContent.LivingTsunamiTriggeredEffect(ArienCardContent.ContentRevision),
                    // Milestone 8.22. What Sabina's Big Sodding Gun runs after one of her Pushes has
                    // moved an enemy hero. It is the first verified content to reach milestone
                    // 8.19c1's after-push window, which until now only the synthetic shape visited.
                    SabinaEffectContent.BigSoddingGunTriggeredEffect(SabinaCardContent.ContentRevision),
                    // Milestone 8.23. What Tigerclaw's Cloak and Daggers runs after any basic action of
                    // his ends. It is the first verified content whose triggered effect asks to repeat
                    // the action it fired for rather than doing something of its own.
                    TigerclawEffectContent.CloakAndDaggersTriggeredEffect(
                        TigerclawCardContent.ContentRevision)
                }));
    }
}

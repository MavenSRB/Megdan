using System;
using System.Collections.Generic;
using System.Linq;
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
                SyntheticEffectCapabilities.CreateCatalog(VerifiedCapabilityHandlers()),
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
                        TigerclawCardContent.ContentRevision),
                    // Milestone 8.24. What Xargatha's Constrict and Final Embrace run at p.8's moment
                    // before the Minion Battle. They are the first verified effects reached from an
                    // Active effect's own trigger rather than from an Ultimate's, and there are two
                    // because the two faces admit different minion classes - which is the whole
                    // difference between the cards.
                    XargathaActiveEffectContent.ConstrictTriggeredEffect.Definition,
                    XargathaActiveEffectContent.FinalEmbraceTriggeredEffect.Definition
                }
                .Concat(
                    // Milestone 8.35z38. Misa's four, joining at the milestone that deals her rather
                    // than at the one that composed them, for the reason every registration in this
                    // file joins at a dealing milestone. Hers is the first group whose members reach
                    // two different windows: Power Overwhelming's discard runs on the after-action
                    // rails when she has placed herself, and her three Green step-moves run at
                    // milestone 8.35z29's `BeforePrimaryAction` checkpoint, which is a window this
                    // build had a producer for and no content behind until now.
                    MisaEffectContent.TriggeredDefinitions(MisaCardContent.ContentRevision))
                .Concat(
                    // Milestone 8.35z46. Silverarrow's three, joining at the milestone that deals her
                    // rather than at the one that composed them, for the same reason. Wild Hunt's
                    // before-action move runs at the window Wasp's High Voltage and Arien's Living
                    // Tsunami both already reach, and her two "End of turn:" discards are the first
                    // verified content to reach the `TimingCheckpoint.TurnEnded` window a card-text
                    // trigger, rather than an Ultimate's or an Active effect's own expiry, fires from.
                    SilverarrowEffectContent.TriggeredEffects(SilverarrowCardContent.ContentRevision))
                // Milestone 8.37. Reign of Winter's own triggered effect - the colour-of-your-choice
                // discard her Ultimate runs each time a basic Attack of hers defeats a minion. It is
                // registered HERE rather than reached through a card node, because a p.16 permanent
                // ability names its effect by reference and the owner walk resolves that reference
                // against this catalog: an unregistered one is a definition the rule can name and the
                // build cannot load, which `VerifiedContentIdentityTests` says out loud.
                .Concat(new[]
                {
                    TaliEffectContent.ReignOfWinterTriggeredEffect(TaliCardContent.ContentRevision),
                    // Milestone 8.37a. Blizzard's End-of-turn repeat, on the same terms: a timing
                    // trigger names its effect by reference, and the owner walk resolves that
                    // reference against this catalog.
                    TaliEffectContent.BlizzardRepeatTriggeredEffect(TaliCardContent.ContentRevision)
                })
                .Concat(
                    // Milestone 8.38e, the milestone that deals Swift the Sharpshooter. Three, on the
                    // terms every group above joins by: Bullet Time's own effect, which p.16's
                    // permanent ability names BY REFERENCE and the owner walk resolves against this
                    // catalog, and the two "End of turn:" placements Delayed Jump and Mobile Scout
                    // stand as Active effects. Hers is the first triggered effect in this build that
                    // GRANTS AN ACTION - milestone 8.38d built the seam that lets a trigger do that at
                    // all - so an unregistered one here would be a grant the rule can name and the
                    // build cannot load.
                    SwiftEffectContent.TriggeredEffects(SwiftCardContent.ContentRevision))
                .Concat(
                    // Milestone 8.39i, the milestone that deals Cutter the Sky Pirate. ONE, on the
                    // terms every group above joins by: Legend of the Skies' own effect, which p.16's
                    // permanent ability names BY REFERENCE and the owner walk resolves against this
                    // catalog - so an unregistered one here would be a definition the rule can name
                    // and the build cannot load. Her Ultimate is the first in this build to fire
                    // after a PRIMARY action rather than after a basic one, which is milestone
                    // 8.39f1's trigger, and the effect it names borrows the previous turn slot's
                    // card through milestone 8.39f's pool.
                    CutterEffectContent.TriggeredEffects(CutterCardContent.ContentRevision))
                .ToArray()));

        /// <summary>
        /// The verified declarations this build contributes to the generic handler set. The order is
        /// the pre-8.25e7 order exactly; <see cref="EffectCapabilityCatalog"/> still owns canonical
        /// registration ordering and duplicate rejection, and this remains one catalog rather than a
        /// per-hero runtime registry.
        ///
        /// <para>Sabina contributes no hero-owned handler: every one of her nodes uses a generic
        /// capability, and her one triggered definition is composed above. That is an explicit empty
        /// contribution, not a missing registration.</para>
        /// </summary>
        private static IEnumerable<IEffectCapabilityHandler> VerifiedCapabilityHandlers() =>
            WaspActiveEffectContent.Handlers()
                .Concat(ArienActiveEffectContent.Handlers())
                .Concat(BroganActiveEffectContent.Handlers())
                // Sabina: zero hero-owned handlers; generic handlers plus her triggered definition.
                .Concat(Enumerable.Empty<IEffectCapabilityHandler>())
                .Concat(TigerclawActiveEffectContent.Handlers())
                .Concat(TigerclawComponentContent.Handlers())
                .Concat(XargathaActiveEffectContent.Handlers())
                // Milestone 8.35z34. Dodger's Weakness and Enfeeblement, joining at her dealing
                // milestone and not at the one that composed them - milestone 7.9's gate refuses a
                // registration no dealt card can start, and until this milestone no configured match
                // dealt her. They are the one group of hers that owed no showcase consumer: both run on
                // ActiveEffectCreationHandler, the type Arien's five persistent texts already proved,
                // and the gate's second half is asked of the handler type.
                .Concat(DodgerActiveEffectContent.Handlers())
                // Milestone 8.35z38. Misa's six creations - three "This turn: Gain +N Defense" guards
                // and three "Next turn: Before you perform a primary action" steps - joining at the
                // milestone that deals her. Like Dodger's pair they owe no showcase consumer: all six
                // run on ActiveEffectCreationHandler, the type Arien's five persistent texts proved,
                // and the deferral gate's second half is asked of the handler type.
                .Concat(MisaActiveEffectContent.Handlers())
                // Milestone 8.35z46. Silverarrow's six creations - three "This turn:" fast-travel and
                // movement restrictions and three "End of turn:"/round-long permissions - joining at
                // the milestone that deals her. They owe no showcase consumer either: all six run on
                // ActiveEffectCreationHandler, the same proved type. Her own new handler type,
                // CardTextFastTravelHandler, is registered separately in
                // `SyntheticRosterCapabilities.RegisteredHandlers` and reaches this build's generic
                // handler set from there, not from here.
                .Concat(SilverarrowActiveEffectContent.Handlers())
                // Milestone 8.35z50. Ursafar's fifteen creations - twelve "This round: You are
                // enraged." grants, two Movement raises and Tear's additional-defeat-cost declaration -
                // joining at the milestone that deals him. Like the three groups above they owe no
                // showcase consumer: every one runs on ActiveEffectCreationHandler, the type Arien's
                // five persistent texts proved, and milestone 7.9's deferral gate asks its second half
                // of the handler TYPE rather than of a capability id. His three genuinely new
                // contracts - `hero-condition@1`, `borrow-primary-action@1` and
                // `attack-target-survived@1` - register from their own Synthetic files rather than from
                // here, because they are generic capabilities this build now owns rather than content
                // of his. He contributes no triggered definition at all: Unbound Fury prints a
                // permanent ability and declares no TriggeredEffect, which is an explicit empty
                // contribution rather than a missing registration.
                .Concat(UrsafarActiveEffectContent.RegisteredHandlers())
                // Milestone 8.35z53. Whisper's own creations, joining at the milestone that deals her.
                // Like the groups above they owe no showcase consumer: they run on
                // ActiveEffectCreationHandler, the type Arien's five persistent texts proved, and
                // milestone 7.9's deferral gate asks its second half of the handler TYPE rather than of
                // a capability id. Her four genuinely new roster contracts - `choose-target-trio@1`,
                // `defended-card-forced-move@1`, `enemy-hero-with-discard-present@1` and
                // `choose-swap-subject@1` - register from SyntheticRosterCapabilities rather than from
                // here, because they are generic capabilities this build now owns rather than content
                // of hers.
                .Concat(WhisperActiveEffectContent.Handlers())
                // Milestone 8.37, the milestone that deals Tali the Spiritcaller. Her six token
                // handlers - a placement and a p.18 short-supply reclamation for each of the three
                // roles of her one supply - are content of HERS rather than generic vocabulary, which
                // is why they register from her own file the way Tigerclaw's Poison marker does and
                // not from a Synthetic one.
                .Concat(TaliEffectContent.RegisteredHandlers())
                // Milestone 8.37a. Her ONE Active effect - Blizzard's End-of-turn clause, the only
                // Active-effect keyword her whole set prints. Milestone 8.37 recorded her as
                // declaring none, which was true of the set it shipped: Blizzard was the face it
                // left deferred.
                .Concat(TaliActiveEffectContent.Handlers())
                // Milestone 8.38e, the milestone that deals Swift the Sharpshooter. Her five Active
                // effects - the two "End of turn:" placements her Blue line stands, and the three
                // counted minion bounties her two Green faces promise - joining on the same terms as
                // every group above. They owe no showcase consumer: every one runs on
                // ActiveEffectCreationHandler, the type Arien's five persistent texts proved, and
                // milestone 7.9's deferral gate asks its second half of the handler TYPE. Her one
                // genuinely new contract, `subject-adjacent-terrain-presence@1`, registers from
                // SyntheticRosterCapabilities rather than from here, because it is generic vocabulary
                // this build now owns rather than content of hers.
                .Concat(SwiftActiveEffectContent.Handlers());

        // Milestone 8.39i, the milestone that deals Cutter the Sky Pirate, adds NOTHING to the list
        // above and says so rather than leaving the absence to be read as an oversight. No face of
        // hers prints "This turn:", "Next turn:" or "End of turn:", so she declares no Active effect
        // at all - which is why there is no `CutterActiveEffectContent` to concat - and her two
        // genuinely new contracts, `synthetic-range-obstacle-target@1` and `claim-solo-victory@1`,
        // register from `SyntheticTargetingCapabilities` and `SyntheticRosterCapabilities` rather
        // than from here, because they are generic vocabulary this build now owns rather than content
        // of hers. Her one triggered definition is composed into `ForThisBuild` above.
    }
}

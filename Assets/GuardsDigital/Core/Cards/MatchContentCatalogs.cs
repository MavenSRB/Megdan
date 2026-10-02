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
                    // Milestone 8.40h. The effect an after-marker-given trigger runs. It is a second
                    // synthetic definition rather than a third user of the one above because what this
                    // window has to prove is different: the other two prove WHEN a trigger runs and
                    // could run any text, and this one proves that the window carries the hero the
                    // marker was given to - so its text has to be one that names that hero.
                    GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                        .AfterMarkerGivenTriggeredEffect.Definition,
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
                .Concat(
                    // Milestone 8.40j, the milestone that deals Bain the Bounty Hunter. TWO, on the
                    // terms every group above joins by. A Complicated Profession's own effect is what
                    // milestone 8.40h's after-a-marker-is-given trigger names BY REFERENCE, and the
                    // owner walk resolves that reference against this catalog - his is the first
                    // permanent ability in the build to watch a MARKER moment rather than an action
                    // or a push. Another One!'s End-of-turn repeat is named by reference too, by the
                    // Active effect that face stands, which is Tali's Blizzard shape exactly.
                    BainEffectContent.TriggeredEffects(BainCardContent.ContentRevision))
                .Concat(new[] { GarrusEffectContent.ResolvedDiscardPrimary(GarrusCardContent.BattleFury, GarrusCardContent.ContentRevision) })
                .Concat(new[] { WidgetEffectContent.DragonKnight(WidgetCardContent.DragonKnight, WidgetCardContent.ContentRevision) })
                .Concat(
                    // Milestone 8.61j, the milestone that deals Min the Dragonmonk as the NINETEENTH
                    // hero. SEVEN, which is the largest group any dealing has added, and they reach
                    // THREE different windows rather than one - the first time that has been true:
                    //
                    //   * `min-mine-trap-1..4` (8.61e) are the blast's consequence list, indexed by
                    //     how many blasts a single crossing removed. They are named by the TOKEN
                    //     DEFINITION rather than by a card node, through
                    //     `TokenDefinition.MoveThroughTrapConsequences`, and the trap operation
                    //     resolves that reference against this catalog - so an unregistered one is a
                    //     price the rule can name and the build cannot load. They run on a route
                    //     somebody else walks on a later turn, which is why they are triggered
                    //     effects rather than steps of the Skill that placed the mine.
                    //   * Both Grenades' end-of-turn bodies (8.61f) are named by reference by the
                    //     Active effects those two faces stand, which is Tali's Blizzard shape.
                    //   * Flurry of Blows' repeat (8.61h) is named by reference by a p.16 permanent
                    //     ability, which is the shape every Ultimate in this list takes.
                    MinComponentContent.TriggeredEffects)
                .Concat(
                    // Milestone 8.71h, the milestone that deals Hanu the Trickster as the TWENTY-FIRST
                    // hero. THREE, one per Blue journey: Unexpected Journey's, There and Back Again's
                    // and Safe Travels' end-of-turn bodies, each named BY REFERENCE by the Active
                    // effect its own face stands - Tali's Blizzard shape, and Min's two Grenades'.
                    //
                    // They are triggered effects rather than card text for the reason p.17 gives: "End
                    // of turn: Swap with that hero regardless of radius and immunity" runs at the END
                    // of the turn that played the card rather than inside the action, so the body is
                    // held by the effect and fired at `TimingCheckpoint.TurnEnded`. An unregistered one
                    // would be a swap back the rule can name and the build cannot load, which is the
                    // half of the journey that the printed sentence makes mandatory.
                    //
                    // His Ultimate adds NOTHING here. The Ultimate Trick runs no effect at all - it is
                    // a standing declaration `DelegatedActionQuery` reads by rule kind, which is
                    // Ursafar's Unbound Fury shape - so there is no fourth entry, and no arm of this
                    // catalog has to learn what "you choose the next action" means.
                    HanuComponentContent.TriggeredEffects)
                .Concat(
                    // Milestone 8.75b, the milestone that deals Ignatia the Mad as the TWENTY-SECOND
                    // hero. ONE: Chaos Incarnate's own body, which p.16's permanent ability names BY
                    // REFERENCE and the owner walk resolves against this catalog - so an unregistered
                    // one would be a definition the rule can name and the build cannot load. It is
                    // the first triggered effect in this build whose first step changes AUTHORITATIVE
                    // ROUND STATE before the step after it runs: it flips the Tie Breaker coin
                    // (`ignatia-flip-tie-breaker@1`, milestone 8.75a) and then repeats the primary
                    // action, so the repeat's own `Conditional` reads the OPPOSITE face and the
                    // repeated performance is the card's other printed text.
                    //
                    // Nothing else of hers is here. Not one of her nineteen faces prints "This turn:",
                    // "Next turn:" or "End of turn:" except Equilibrium's "This round:", and that one
                    // is an `ActiveEffectCreationHandler` in the capability list below rather than a
                    // triggered body - it runs no effect at all, it stands a flag the coin predicate
                    // reads.
                    IgnatiaEffectContent.TriggeredEffects(IgnatiaCardContent.ContentRevision))
                .Concat(new[] { SnorriUltimateContent.TriggeredEffect })
                // Milestone 8.112g, the milestone that deals Mrak the Rockshaper as the TWENTY-FIFTH
                // hero. ONE: Rock and a Hard Place's discard, which its after-tokens-placed declaration
                // names BY REFERENCE (8.112f).
                .Concat(MrakCardContent.TriggeredEffects())
                // Milestone 8.114f, the milestone that deals Wuk the Grove Keeper as the TWENTY-SIXTH
                // hero: March of Nature's placement and Dominance's two pre-battle exclusions.
                .Concat(WukCardContent.TriggeredEffects())
                // Milestone 9.21j, the milestone that deals Razzle the Ringmaster as the
                // TWENTY-EIGHTH hero: Twin Strike's "a different you may repeat it once", the body a
                // p.16 triggered ability names by reference (9.21h).
                .Concat(new[] { RazzleEffectContent.TwinStrikeTriggeredEffect(RazzleCardContent.ContentRevision) })
                .Concat(new[]
                {
                    TrinketsEffectContent.DisruptorTrigger(
                        TrinketsCardContent.DisruptorJolt,
                        TrinketsCardContent.ContentRevision, false),
                    TrinketsEffectContent.DisruptorTrigger(
                        TrinketsCardContent.DisruptorPulse,
                        TrinketsCardContent.ContentRevision, false),
                    TrinketsEffectContent.DisruptorTrigger(
                        TrinketsCardContent.DisruptorGrid,
                        TrinketsCardContent.ContentRevision, true)
                })
                .Concat(new[]
                {
                    // Milestone 8.71h. What the Red Wayfarer's Hero-scoped end-of-turn effect runs -
                    // the showcase consumer milestone 7.9's gate asks of `hanu-journey-partner@1`,
                    // which is the one contract of his that cannot be proved by a card naming it:
                    // the lookup is ABOUT an occurrence, so a Skill with no occurrence behind it
                    // would prove only the refusal. It joins the one catalog rather than forming a
                    // second, for the reason 8.19a's before-action body above gives.
                    GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                        .JourneyShowcaseTriggeredEffect.Definition
                })
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
                .Concat(SwiftActiveEffectContent.Handlers())
                // Milestone 8.40j, the milestone that deals Bain the Bounty Hunter. Two groups, and
                // the split is the one this file has drawn since Tigerclaw's Poison. His FIVE Active
                // effects - the two obstacle permissions and the two Movement raises his two Movement
                // primaries stand, and Another One!'s End-of-turn repeat - owe no showcase consumer:
                // every one runs on ActiveEffectCreationHandler, the type Arien's five persistent
                // texts proved at milestone 6.8, and milestone 7.9's deferral gate asks its second
                // half of the handler TYPE.
                .Concat(BainActiveEffectContent.Handlers())
                // And his FOUR marker operations - the give, the transfer, the definition-bound
                // return and the holder predicate all five of his Green faces read - which are
                // content of HIS rather than generic vocabulary, which is why they register from his
                // own file exactly as Tigerclaw's two gives do and not from a Synthetic one. Two of
                // the four are new handler TYPES and each took a showcase consumer in this same
                // change, bound to the fixture's Focus marker rather than to his bounty.
                .Concat(BainComponentContent.Handlers())
                .Concat(GarrusActiveEffectContent.Handlers())
                .Concat(WidgetComponentContent.RegisteredHandlers())
                // Milestone 8.61j, the milestone that deals Min the Dragonmonk. SEVENTEEN handlers,
                // which is the largest group a dealing has contributed from a hero's own file, and
                // they are content of HERS rather than generic vocabulary - which is why they
                // register from here exactly as Tali's six token operations and Bain's four marker
                // ones do: FIVE Smoke bomb operations (presence, unbounded space, the two printed-icon
                // spaces, the supply Place), TWO mine placements and ONE group reclaim, THREE grenade
                // operations and TWO anchored hero selections, and her TWO end-of-turn Active
                // effects and TWO doubling ones.
                //
                // NOT EVERY CONTRACT HER SERIES DECLARED IS HERE, and the two that are missing are
                // missing on purpose: `min-place-smoke-bomb-in-range@1` and `min-reclaim-grenade@1`
                // are reached by no composed sentence of hers, so this milestone DROPPED them rather
                // than registering a contract nothing can start - which is what milestone 7.9's gate
                // asks and what a permanent deferred list would only have said more quietly.
                //
                // Her two genuinely new contracts are NOT here for the opposite reason:
                // `borrow-after-attack-text@1` registers from
                // `SyntheticBorrowedCardTextCapabilities` and `take-deck-card-as-item@1` from
                // `SyntheticRosterCapabilities`, because they are generic vocabulary this build now
                // owns rather than content of hers. Both are new handler TYPES, so each took a
                // showcase consumer in this same change.
                .Concat(MinComponentContent.RegisteredHandlers())
                // Milestone 8.69g, the milestone that deals Brynn the Seeker as the TWENTIETH hero.
                // THREE handlers, which is the narrowest group any dealing since Rowenna's has
                // contributed, and every one of them is content of HIS rather than generic vocabulary -
                // which is why they register from here exactly as Min's seventeen and Bain's four do:
                //
                //   * `brynn-enemy-hero-in-radius-adjacent-to-obstacles@1` (8.69a), the presence
                //     predicate Mountain Guide, Expedition Leader, True Grit and Die Hard gate their
                //     second sentence on. It is hero-scoped rather than a widening of
                //     `subject-adjacent-terrain-presence@1`, because that contract answers about p.18's
                //     terrain and this one about a THRESHOLD over p.18's whole obstacle list.
                //   * `brynn-attack-target-was-adjacent-to-obstacles@1` (8.69b), the question Split
                //     Attack, Split Throw and Peak Precision ask of the Attack that is running - about
                //     the TARGETING rather than about a hero, which is why p.14's step-6 removal of a
                //     defeated target does not turn the answer over.
                //   * `brynn-tracking-swap-subject@1` (8.69d), the one selection Tread Lightly, Cover
                //     Tracks and Hide Traces choose over: the union of p.19's ring and the enemy heroes
                //     in the performing card's printed Radius who satisfy his predicate.
                //
                // ALL THREE ARE NEW HANDLER TYPES, so each took a showcase consumer in this same
                // change - the Red Surveyor, the Red Quarryman's rider and the Red Pathkeeper. NOTHING
                // ELSE OF HIS REGISTERS: he places no token, attaches no marker and holds no triggered
                // effect (his shard records that outright), and his Ultimate is a permanent ability
                // whose whole content is a standing rule declaration `UltimateUnlockQuery` derives -
                // Rowenna's and Whisper's shape - so there is no `BrynnActiveEffectContent` and no
                // triggered-effect concat above.
                .Concat(BrynnComponentContent.RegisteredHandlers())
                // Milestone 8.71h, the milestone that deals Hanu the Trickster as the TWENTY-FIRST
                // hero. SIX handlers behind THREE new types, which is the first dealing since Min's
                // whose two counts differ because a hero stands Active effects of his own:
                //
                //   * `hanu-hurry-up-initiative@1` (8.71a), the rewrite his Silver performs. It is the
                //     first contract in this build that REPLACES a printed value rather than adding to
                //     one, and the first keyed on a CARD INSTANCE rather than on a hero - which is why
                //     it could not be a widening of anything `InitiativeResolutionAuthority` already
                //     composed.
                //   * `hanu-journey-partner@1` (8.71d), the "that hero" his three journeys' end-of-turn
                //     bodies re-derive from the lifecycle at fire time rather than carry - so the
                //     referent survives snapshot, replay and rewind with no payload growth.
                //   * `hanu-friendly-unit-pair@1` (8.71f), the one selection Monkey Trick, Monkey Twist
                //     and Monkey Business choose over: two units of his own team inside the performing
                //     card's printed Radius, himself excluded by p.17. `choose-target-pair@1` could not
                //     carry it - that contract's candidates are minions and its reach is a literal
                //     distance where his Greens print a Radius icon with an item on it.
                //
                // ALL THREE ARE NEW HANDLER TYPES, so each took a showcase consumer in this same change
                // - the Red Hastener, the Red Wayfarer and the Red Troupemaster.
                //
                // The other three are `ActiveEffectCreationHandler` registrations over the three
                // definitions in `HanuActiveEffectContent`, one per journey face, reached ONCE through
                // `JourneyRegisteredHandlers` rather than concatenated a second time from here -
                // milestone 8.40j's BA-18, "Capability references must be unique". They owe no showcase
                // consumer: that handler type was proved by Arien's five persistent texts, and the gate
                // asks its second half of the TYPE.
                //
                // He places no token and gives no marker, so nothing of his reaches the component
                // catalog, and his Ultimate is a permanent declaration rather than content - see the
                // triggered-effect list above.
                .Concat(HanuComponentContent.RegisteredHandlers())
                // Milestone 8.75b, the milestone that deals Ignatia the Mad as the TWENTY-SECOND
                // hero. ELEVEN handlers behind SIX new types, which is the widest gap between the two
                // counts any dealing has opened - and the reason is that her set is built on ONE
                // predicate every card branches on rather than on many small ones:
                //
                //   * `ignatia-coin-sun@1` (8.75) is that predicate. It reads the live Tie Breaker
                //     face off `EffectCapabilityContext.TieBreakerRevealedSide` and publishes one
                //     boolean, and every one of her eighteen dealt bodies is a single `Conditional`
                //     over it - sun text then, moon text else. No dealt hero before her reads the coin
                //     from inside an action at all.
                //   * `ignatia-equilibrium@1` (8.75) is an `ActiveEffectCreationHandler` over the
                //     round-scoped marker her Silver stands. It owes no showcase consumer - Arien's
                //     five persistent texts proved that type at 6.8 - but the PREDICATE above does,
                //     because while the marker stands the predicate stops reading the coin and asks
                //     the actor which face to apply.
                //   * `ignatia-flip-tie-breaker@1` (8.75a) turns the match's coin over from inside a
                //     card action. It publishes `TieBreakerFlipQuery.FlippedOutput` rather than acting,
                //     because a capability context carries no continuation and the coin is the round
                //     authority's; the authority applies the flip before the next node runs, which is
                //     the whole of why the Ultimate's repeat reads the opposite text.
                //   * `ignatia-range-unit-in-straight-line@1`, `...-not-in-straight-line@1`,
                //     `...-at-maximum-range@1` and `ignatia-range-hero-not-adjacent@1` (8.75) are her
                //     four card-text ATTACK subjects, all on one handler type: the coin picks the
                //     target SET on every attack she owns, so p.14 reaches step 1 with no target and
                //     the body binds one through `set-attack-target@1` at
                //     `CardTextTiming.BeforeTheAttack`.
                //   * `ignatia-enemy-hero-adjacent-to-magma-or-minion@1` and
                //     `ignatia-enemy-minion-adjacent-to-enemy-hero@1` (8.75) are the two subjects her
                //     Blue discard line names, and neither is a narrowing of a shared selection: one
                //     asks about a hero's neighbours being EITHER a token or a minion, the other about
                //     a minion's neighbours being an enemy HERO.
                //   * `ignatia-place-magma-in-radius@1` is milestone 8.37's shared `PlaceHandler` bound
                //     to her magma definition - Min's mines' shape exactly, adding a registration and
                //     no type - and `ignatia-place-magma-wake@1` is the one placement this build had no
                //     form of: a token dropped in every empty space a straight-line Move passed through
                //     or left.
                //
                // SIX of the eleven are new handler TYPES and each took a showcase consumer in this
                // same change - the Red Coinreader, the Red Coinflipper, the Red Longshot, the Red
                // Inquisitor and the Red Trailblazer - which is milestone 7.9's gate's second half.
                // Her magma TOKEN definition joins the component catalog in the same edit, because the
                // wake showcase places it and a showcase that named an unregistered definition would
                // prove only the refusal.
                .Concat(IgnatiaComponentContent.RegisteredHandlers())
                .Concat(TrinketsComponentContent.ObstacleHandlers())
                .Concat(TrinketsComponentContent.BarrierHandlers())
                .Concat(TrinketsActiveEffectContent.Handlers())
                .Concat(SnorriComponentContent.Handlers())
                .Concat(SnorriAttackTargetContent.Handlers())
                .Concat(SnorriBlueActiveEffectContent.Handlers())
                .Concat(SnorriBlueSupportCapabilities.Handlers())
                .Concat(SnorriGreenBlockContent.Handlers())
                .Concat(new IEffectCapabilityHandler[]
                {
                    new SnorriGreenEffectContent.SelectRuneHandler()
                })
                .Concat(SnorriUltimateContent.Handlers())
                // Milestone 8.112g. Mrak's six placement steps and two Carapace grants.
                .Concat(MrakComponentContent.RegisteredHandlers())
                // Milestone 8.114f. Wuk's Tree, paired swap, Dominance and trampling contracts.
                .Concat(WukComponentContent.RegisteredHandlers())
                // Milestone 9.8f, the milestone that deals Mortimer the Awakener as the
                // TWENTY-SEVENTH hero: the Zombie's placement, short supply and minion Replace, and
                // Knife of the Living Dead's per-pass "+1 Attack".
                .Concat(MortimerComponentContent.Handlers())
                // Milestone 9.21j, the milestone that deals Razzle the Ringmaster as the
                // TWENTY-EIGHTH hero: the spawn of one body (9.21d), the removal of his bodies and the
                // "more than one of you" gate (9.21e), "another one of you" and the retreat's line end
                // (9.21f), and "other you in radius" (9.21g) - six handler types, each new to the build.
                .Concat(RazzleEffectContent.Handlers())
                // Milestone 9.30i, the milestone that deals Gydion the Archwizard as the
                // TWENTY-NINTH hero: the twenty-two spells' naming steps and the Familiar's three
                // (9.30e), Expeditious Retreat's straight-line walk (9.30d), Shield's and
                // Invulnerability's immunities (9.30f) and Wish's presence predicate (9.30g). The naming
                // step and the presence predicate are the two handler types new to the build, each bound
                // to one spell's definition and proved on synthetic spells and staged rounds by
                // 9.30c-9.30g (no showcase card can own a spellbook), the precedent Razzle's hero-only
                // types set; the rest are declarations over types already registered.
                .Concat(GydionComponentContent.Handlers())
                .Concat(GydionCardContent.ReuseSpellHandlers())
                .Concat(GydionCardContent.ImmunityDiscardLifeMoveHandlers())
                .Concat(GydionCardContent.ArchwizardHandlers())
                // Milestone 9.32h, the milestone that deals Takahide the Warlord as the THIRTIETH hero:
                // Spinning Blade's and Blade Helix's enemy-obstacle creations (9.32c, 9.32d), ordinary
                // `ActiveEffectCreationHandler`s over 9.32c's unit overlay.
                .Concat(TakahideCardContent.Handlers())
                // Milestone 8.71h. The ONE creation the showcase owns rather than a hero: the
                // Hero-scoped end-of-turn effect the Red Wayfarer stands so that
                // `hanu-journey-partner@1` has an occurrence to read. It is an ordinary
                // `ActiveEffectCreationHandler`, the type Arien's five persistent texts proved, so it
                // adds a registration without adding a type - and it is registered from here for the
                // same reason `SyntheticCoupledMoveCapabilities` below is: the synthetic catalog's own
                // groups are composed before any showcase card exists.
                .Concat(GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                    .RegisteredHandlers())
                .Concat(SyntheticCoupledMoveCapabilities.RegisteredHandlers());

        // Milestone 8.39i, the milestone that deals Cutter the Sky Pirate, adds NOTHING to the list
        // above and says so rather than leaving the absence to be read as an oversight. No face of
        // hers prints "This turn:", "Next turn:" or "End of turn:", so she declares no Active effect
        // at all - which is why there is no `CutterActiveEffectContent` to concat - and her two
        // genuinely new contracts, `synthetic-range-obstacle-target@1` and `claim-solo-victory@1`,
        // register from `SyntheticTargetingCapabilities` and `SyntheticRosterCapabilities` rather
        // than from here, because they are generic vocabulary this build now owns rather than content
        // of hers. Her one triggered definition is composed into `ForThisBuild` above.
        //
        // Milestone 8.40j's own two generic contracts follow the same rule and are named here for the
        // same reason: `synthetic-hidden-card-choice@1` and `synthetic-reveal-chosen-card@1` register
        // from `SyntheticHiddenChoiceCapabilities` through `SyntheticEffectCapabilities.CreateCatalog`
        // rather than from here, because they are vocabulary this build now owns rather than content
        // of his, and the two showcase marker instances register from `SyntheticComponentCapabilities`
        // beside the give they mirror.
        //
        // Milestone 8.57g, the milestone that deals Rowenna the Vanguard as the EIGHTEENTH hero, adds
        // nothing to either list above and says so rather than leaving the absence to be read as an
        // oversight. It is the emptiest contribution any dealing has made, and every half of it was
        // measured rather than assumed:
        //
        //   * NO ACTIVE EFFECT. Not one of her nineteen faces prints "This turn:", "Next turn:" or
        //     "End of turn:", so there is no `RowennaActiveEffectContent` to concat - the same
        //     explicit empty contribution Cutter's milestone records.
        //   * NO TRIGGERED DEFINITION. Fabled Lance is a p.16 PERMANENT ABILITY whose
        //     `UltimateRuleDefinition.TriggeredEffect` is null: it names no effect to run, because
        //     what it does is change a rule (`RuleModificationKind.AttackActionsCountAsRangedAt-
        //     PrintedRange`, milestone 8.57e) for as long as it is unlocked. A rule modification is
        //     carried by the card definition itself and resolved through `UltimateUnlockQuery`, so it
        //     costs this catalog nothing - which is why registering her moves the registered-handler
        //     COUNT and not the triggered-effect list.
        //   * NO HERO-OWNED HANDLER. Every node her five composition blocks wrote binds a generic
        //     capability this build already registered, which is milestone 8.57a-8.57e's whole record.
        //
        // What her dealing DOES move is `SyntheticPlacementCapabilities`: milestone 8.57b's
        // `synthetic-place-in-range-beside-a-named-hero@1` and milestone 8.57d's
        // `synthetic-place-in-radius-beside-a-named-hero@1` leave that file's `DeferredHandlers` for
        // its `RegisteredHandlers` in this same change, because Throw the Gauntlet, Close Quarters,
        // Melee and Grand Melee are cards a configured match now puts in a hand.
    }
}

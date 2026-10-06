using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e7. Generic effect handlers may implement a verified declaration, but the
    /// generic catalog layer may not decide which hero declarations this build owns. That dependency
    /// points from build composition to both sides, never from Core.Effects back into verified cards.
    /// </summary>
    public sealed class EffectCatalogCompositionTests
    {
        [Test]
        public void CoreEffectsDoesNotReferenceVerifiedCardContent()
        {
            var subject = ILConformanceScanner.TypesIn(typeof(SyntheticEffectCapabilities).Assembly)
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                    "GuardsDigital.Core.Effects",
                    StringComparison.Ordinal))
                .ToArray();
            var forbidden = VerifiedContentTypes()
                .SelectMany(type => new MemberInfo[] { type }.Concat(type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
                .ToArray();

            Assert.That(forbidden, Is.Not.Empty,
                "The compiled verified-content member set must be nonempty, or this dependency scan " +
                "would prove nothing.");
            Assert.That(ILConformanceScanner.UnresolvedTokens(subject), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a verified-content " +
                "dependency could hide behind one.");

            var violations = ILConformanceScanner.FindForbiddenReferences(
                subject,
                forbidden,
                "generic Core.Effects code references verified hero content");
            Assert.That(violations, Is.Empty,
                "Verified handler declarations are supplied by MatchContentCatalogs; generic effect " +
                "code implements contracts and never imports a hero:\n" +
                string.Join("\n", violations));
        }

        [Test]
        public void BuildCompositionPreservesOneOrderedCatalogAndExactCapabilityIdentity()
        {
            var generic = SyntheticEffectCapabilities.CreateCatalog();
            var verified = VerifiedHandlers().ToArray();
            var expected = generic.Registered
                .Concat(verified.Select(handler => handler.Reference))
                .OrderBy(reference => reference.CapabilityId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray();
            var first = MatchContentCatalogs.ForThisBuild().Capabilities;
            var second = MatchContentCatalogs.ForThisBuild().Capabilities;

            Assert.That(first.Registered, Is.EqualTo(expected),
                "The build catalog is exactly the generic implementation set plus its verified " +
                "content declarations; no second registry or discovery set exists.");
            Assert.That(second.Registered, Is.EqualTo(first.Registered),
                "Registration order is deterministic across fresh build compositions.");
            foreach (var handler in verified)
            {
                Assert.That(first.Load(handler.Reference).GetType(), Is.EqualTo(handler.GetType()),
                    $"Capability identity '{handler.Reference}' must still load its declared handler.");
            }

            var missing = Assert.Throws<EffectCapabilityLoadException>(() => first.Load(
                new EffectCapabilityReference(
                    new EffectCapabilityId("not-registered-by-this-build"), 1)));
            Assert.That(missing.Reason,
                Is.EqualTo(EffectCapabilityLoadRejectionReason.MissingCapability));
        }

        private static IReadOnlyList<Type> VerifiedContentTypes() =>
            typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type => string.Equals(
                    type.Namespace,
                    typeof(WaspCardContent).Namespace,
                    StringComparison.Ordinal))
                .ToArray();

        private static IEnumerable<IEffectCapabilityHandler> VerifiedHandlers() =>
            WaspActiveEffectContent.Handlers()
                .Concat(ArienActiveEffectContent.Handlers())
                .Concat(BroganActiveEffectContent.Handlers())
                .Concat(TigerclawActiveEffectContent.Handlers())
                .Concat(TigerclawComponentContent.Handlers())
                .Concat(XargathaActiveEffectContent.Handlers())
                // Milestone 8.35z34. Dodger's Weakness and Enfeeblement, joining the moment her
                // dealing milestone put them into MatchContentCatalogs - which this list deliberately
                // mirrors by hand rather than calling, because a mirror that called the production
                // composer would agree with it by construction and prove nothing.
                .Concat(DodgerActiveEffectContent.Handlers())
                // Milestone 8.35z38. Misa's six - three "This turn" Defense guards and three "Next
                // turn" step-moves - joining the moment her dealing milestone put them into
                // MatchContentCatalogs, and mirrored by hand here for the same reason Dodger's pair is.
                .Concat(MisaActiveEffectContent.Handlers())
                // Milestone 8.35z46. Silverarrow's six - three "This turn" fast-travel/movement
                // restrictions and three "End of turn:"/round-long permissions - joining the moment
                // her dealing milestone put them into MatchContentCatalogs, mirrored by hand here for
                // the same reason. Her own new handler type, CardTextFastTravelHandler, is not part of
                // this list: it is a roster capability registered in
                // SyntheticRosterCapabilities.RegisteredHandlers, not a hero-owned Active-effect
                // creation, and this mirror is of VerifiedCapabilityHandlers alone.
                .Concat(SilverarrowActiveEffectContent.Handlers())
                // Milestone 8.35z50. Ursafar's fifteen - twelve "This round: You are enraged." grants,
                // two Movement raises and Tear's additional-defeat-cost declaration - joining the moment
                // his dealing milestone put them into MatchContentCatalogs, and mirrored BY HAND here
                // for the reason Dodger's pair records: a mirror that called the production composer
                // would agree with it by construction and prove nothing. His three genuinely new
                // contracts are NOT in this list - `hero-condition@1`, `borrow-primary-action@1` and
                // `attack-target-survived@1` are generic capabilities registered from their own
                // Synthetic files, and this mirror is of VerifiedCapabilityHandlers alone, exactly as
                // Silverarrow's CardTextFastTravelHandler is excluded above.
                .Concat(UrsafarActiveEffectContent.RegisteredHandlers())
                // Milestone 8.35z53. Whisper's one - Death Seeker's turn-long immunity to enemy hero
                // actions (RULING 33) - joining the moment her dealing milestone put it into
                // MatchContentCatalogs, and mirrored BY HAND here for the reason Dodger's pair records.
                // Her FOUR genuinely new contracts are NOT in this list - `choose-target-trio@1`,
                // `defended-card-forced-move@1`, `enemy-hero-with-discard-present@1` and
                // `choose-swap-subject@1` are generic capabilities registered from
                // SyntheticRosterCapabilities, and this mirror is of VerifiedCapabilityHandlers alone,
                // exactly as Silverarrow's CardTextFastTravelHandler is excluded above.
                .Concat(WhisperActiveEffectContent.Handlers())
                // Milestone 8.37. Tali's SIX token handlers - a placement and a rulebook p.18
                // short-supply reclamation for each of the three roles of her one pile - mirrored by
                // hand for the reason Dodger's pair records. She is the first hero whose contribution
                // here is not ONLY an Active-effect list: what these six are is content-of-hers bound
                // to her own token definitions, the way Tigerclaw's Poison marker is. Her EIGHT
                // genuinely new generic contracts are NOT in this list - the six from
                // SyntheticCondition/BorrowedAction/Displacement/Roster and the two showcase token
                // instances from SyntheticTokenCapabilities are all registered from their own
                // Synthetic files, and this mirror is of VerifiedCapabilityHandlers alone, exactly as
                // Silverarrow's CardTextFastTravelHandler is excluded above.
                .Concat(TaliEffectContent.RegisteredHandlers())
                // Milestone 8.37a. Her ONE Active effect, and the reason milestone 8.37 recorded her
                // as declaring none: her whole set's only Active-effect keyword is Blizzard's
                // `End of turn:`, and Blizzard was the one face that milestone left deferred.
                .Concat(TaliActiveEffectContent.Handlers())
                // Milestone 8.38e. Swift's FIVE - the two "End of turn:" placements her Blue line
                // stands and the three counted minion bounties her two Green faces promise - joining
                // the moment her dealing milestone put them into MatchContentCatalogs, and mirrored BY
                // HAND here for the reason Dodger's pair records: a mirror that called the production
                // composer would agree with it by construction and prove nothing. Her ONE genuinely new
                // contract is NOT in this list - `subject-adjacent-terrain-presence@1` is a generic
                // capability registered from SyntheticRosterCapabilities, and this mirror is of
                // VerifiedCapabilityHandlers alone, exactly as Silverarrow's CardTextFastTravelHandler
                // is excluded above. Nor are her two straight-line Places, which register from
                // SyntheticPlacementCapabilities on the same terms.
                .Concat(SwiftActiveEffectContent.Handlers())
                // Milestone 8.40j. Bain's NINE - his five Active-effect creations (two obstacle
                // permissions, two Movement raises and Another One!'s End-of-turn repeat) and his
                // four marker operations (the give, the transfer, the definition-bound return and
                // the holder predicate his five Green faces read) - joining the moment his dealing
                // milestone put them into MatchContentCatalogs, and mirrored BY HAND here for the
                // reason Dodger's pair records: a mirror that called the production composer would
                // agree with it by construction and prove nothing. He is the second hero whose
                // contribution here is not ONLY an Active-effect list, on Tali's terms and
                // Tigerclaw's: his four marker operations are content of HIS, bound to his own marker
                // definition. His FOUR genuinely generic contracts are NOT in this list -
                // `synthetic-hidden-card-choice@1` and `synthetic-reveal-chosen-card@1` register from
                // SyntheticHiddenChoiceCapabilities and the two showcase marker instances
                // (`synthetic-transfer-marker@1`, `synthetic-marker-held@1`) from
                // SyntheticComponentCapabilities, and this mirror is of VerifiedCapabilityHandlers
                // alone, exactly as Silverarrow's CardTextFastTravelHandler is excluded above.
                .Concat(BainActiveEffectContent.Handlers())
                .Concat(BainComponentContent.Handlers())
                .Concat(GarrusActiveEffectContent.Handlers())
                .Concat(WidgetComponentContent.RegisteredHandlers())
                // Milestone 8.61j. Min's SEVENTEEN - five Smoke bomb operations, two mine placements
                // and one group reclaim, three grenade operations, two anchored hero selections and
                // four Active-effect creations (both Grenades' end-of-turn arming and both doubling
                // faces) - joining the moment her dealing milestone put them into
                // MatchContentCatalogs, and mirrored BY HAND here for the reason Dodger's pair
                // records: a mirror that called the production composer would agree with it by
                // construction and prove nothing. She is the third hero whose contribution here is
                // not ONLY an Active-effect list, on Tali's terms and Bain's: her token operations
                // are content of HERS, bound to her own token definitions. Her TWO genuinely generic
                // contracts are NOT in this list - `borrow-after-attack-text@1` registers from
                // SyntheticBorrowedCardTextCapabilities and `take-deck-card-as-item@1` from
                // SyntheticRosterCapabilities, and this mirror is of VerifiedCapabilityHandlers
                // alone, exactly as Silverarrow's CardTextFastTravelHandler is excluded above.
                .Concat(MinComponentContent.RegisteredHandlers())
                // Milestone 8.69g. Brynn's THREE - the obstacle-radius presence predicate, the
                // question his three split faces ask of the running Attack, and the union selection
                // his three swaps choose over - joining the moment his dealing milestone put them
                // into MatchContentCatalogs, and mirrored BY HAND here for the reason Dodger's pair
                // records: a mirror that called the production composer would agree with it by
                // construction and prove nothing. He contributes NO Active effect and NO token, so
                // all three of his are ordinary capability handlers of his own.
                .Concat(BrynnComponentContent.RegisteredHandlers())
                // Milestone 8.71h. Hanu's SIX - the printed-Initiative rewrite his Silver performs,
                // the partner lookup his three journeys' end-of-turn bodies re-derive, the friendly
                // pair his Greens exchange, and the three Active-effect creations those journeys arm
                // - joining the moment his dealing milestone put them into MatchContentCatalogs, and
                // mirrored BY HAND here for the reason Dodger's pair records: a mirror that called
                // the production composer would agree with it by construction and prove nothing.
                .Concat(HanuComponentContent.RegisteredHandlers())
                // Milestone 8.75b. Ignatia's ELEVEN - the coin predicate every one of her dealt
                // faces branches on, Equilibrium's round-long marker, Chaos Incarnate's flip, the
                // FOUR card-text Attack subjects her Reds name, her Blue line's two discard
                // subjects and her two magma placements - joining the moment her dealing milestone
                // put them into MatchContentCatalogs, and mirrored BY HAND here for the reason
                // Dodger's pair records: a mirror that called the production composer would agree
                // with it by construction and prove nothing. She is the fourth hero whose
                // contribution here is not ONLY an Active-effect list, on Tali's terms, Bain's and
                // Min's: her magma placements are content of HERS, bound to her own token
                // definition. Nothing of hers registers from a `Synthetic*Capabilities` file at all,
                // which makes her the first dealing since Brynn's with nothing to exclude here.
                .Concat(IgnatiaComponentContent.RegisteredHandlers())
                // Trinkets' obstacle, Barrier and Disruptor declarations are mirrored independently
                // of MatchContentCatalogs, so an omitted registration fails this check.
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
                .Concat(MrakComponentContent.RegisteredHandlers())
                .Concat(WukComponentContent.RegisteredHandlers())
                .Concat(MortimerComponentContent.Handlers())
                .Concat(RazzleEffectContent.Handlers())
                // Milestone 9.30i. Gydion's naming steps and Familiar, Expeditious Retreat's walk,
                // Shield's and Invulnerability's immunities and Wish's presence predicate, mirrored by
                // hand from the milestone that deals him.
                .Concat(GydionComponentContent.Handlers())
                .Concat(GydionCardContent.ReuseSpellHandlers())
                .Concat(GydionCardContent.ImmunityDiscardLifeMoveHandlers())
                .Concat(GydionCardContent.ArchwizardHandlers())
                // Milestone 9.32h. Takahide's two enemy-obstacle creations.
                .Concat(TakahideCardContent.Handlers())
                // Milestone 9.49. Mirror Emmitt's complete verified registrations independently.
                .Concat(EmmittActiveEffectContent.Handlers())
                .Concat(EmmittComponentContent.Handlers())
                .Concat(EmmittGlitchChoiceContent.Handlers())
                .Concat(EmmittTimeWarpContent.Handlers())
                .Concat(EmmittTimeCapsuleContent.Handlers())
                .Concat(EmmittBackToFutureContent.Handlers())
                // Milestone 9.53. Mirror NebKher's component, delayed-color and Ultimate
                // declarations independently of the production catalog composer.
                .Concat(NebKherComponentContent.Handlers())
                .Concat(NebKherDelayedDoubtContent.Handlers())
                .Concat(NebKherUltimateContent.Handlers())
                .Concat(new[]
                {
                    SyntheticPreviousSlotBorrowCapabilities.Handler(),
                    SyntheticResolvedCardOrderSwapCapabilities.Handler()
                })
                // and the ONE creation the SHOWCASE owns rather than a hero: the Hero-scoped
                // end-of-turn effect the Red Wayfarer stands so `hanu-journey-partner@1` has an
                // occurrence to read. It registers beside the verified declarations rather than with
                // the synthetic groups because the synthetic catalog's own groups are composed before
                // any showcase card exists.
                .Concat(GuardsDigital.Core.Cards.Synthetic.SyntheticShowcaseHand
                    .RegisteredHandlers())
                .Concat(SyntheticCoupledMoveCapabilities.RegisteredHandlers());
    }
}

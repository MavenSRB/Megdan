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
                .Concat(TaliActiveEffectContent.Handlers());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.28b acceptance check 4, stated as a rule rather than as a promise.
    ///
    /// The block asks that "future randomness consumers have one seam to join; no second unrecorded
    /// randomness source is introduced". A sentence in a document cannot hold that: the second
    /// consumer arrives in a milestone that is not reading this one, and an unseeded draw is four
    /// characters of ordinary-looking code. So the claim is held mechanically - the authoritative
    /// assemblies may reach the platform's entropy from exactly one member, and that member records
    /// what it drew.
    ///
    /// Presentation is deliberately out of scope. A view that animates from a clock or a jitter is not
    /// making a rules decision, and forbidding it would say something this milestone has no evidence
    /// for. What matters is that no authoritative answer depends on a number nobody wrote down.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class SetupRandomnessConformanceTests
    {
        [Test]
        public void NothingButTheSetupRandomnessSeam_ReachesUnrecordedEntropy()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(subjects, Is.Not.Empty,
                "A rule with no subjects passes by having nothing to check.");

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindUnrecordedDraws(subjects), Is.Empty,
                "An authoritative type is drawing from a source nothing records. Every setup decision " +
                "that needs randomness derives from SetupRandomnessAuthority's recorded seed, so the " +
                "decision can be made again and reach the same answer - which is what deterministic " +
                "replay and a future online authority both require.");
        }

        [Test]
        public void TheSeam_DoesDrawOne()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(SetupRandomnessAuthority)),
                    ForbiddenMembers().ToArray()[0]),
                Is.True,
                "A rule forbidding every caller is satisfied by an engine that draws nothing at all, so " +
                "the one permitted draw must be shown to be there. Normal setup still flips randomly " +
                "from the player's perspective; only its provenance changed.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindUnrecordedDraws(new[] { typeof(UnrecordedDrawSample) }), Is.Not.Empty,
                "A type reaching platform entropy directly must be reported, or the rule is inert.");

            Assert.That(FindUnrecordedDraws(new[] { typeof(RecordedDerivationSample) }), Is.Empty,
                "A type deriving from a recorded seed must not be reported, or the rule bans the " +
                "ordinary shape it exists to permit.");
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(ForbiddenMembers().ToArray(), Is.Not.Empty,
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindUnrecordedDraws(IEnumerable<Type> types)
        {
            var forbidden = ForbiddenMembers().ToArray();
            return types
                .Where(type => !IsPermittedDrawer(type))
                .SelectMany(type => ILConformanceScanner
                    .FindForbiddenReferences(new[] { type }, forbidden, "reaches unrecorded entropy"))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// The one seam. It is excluded because the rule is about who may draw, not about drawing
        /// existing: a normal match still flips a real coin.
        /// </summary>
        private static bool IsPermittedDrawer(Type type) =>
            type == typeof(SetupRandomnessAuthority) ||
            type.DeclaringType == typeof(SetupRandomnessAuthority);

        /// <summary>
        /// The platform's unrecorded sources, resolved as members rather than named as strings, so a
        /// rename fails loudly here instead of quietly disarming the rule.
        /// </summary>
        private static IEnumerable<MemberInfo> ForbiddenMembers()
        {
            yield return ILConformanceScanner.Method(
                typeof(RandomNumberGenerator), "Create", Type.EmptyTypes);
            yield return ILConformanceScanner.Constructor(typeof(Random), Type.EmptyTypes);
            yield return ILConformanceScanner.Method(typeof(Guid), nameof(Guid.NewGuid), Type.EmptyTypes);
        }

        /// <summary>
        /// Every type in the authoritative assemblies, so a source written after this milestone is
        /// audited by existing rather than by being added to a list. The namespace filter matters in
        /// the cross-platform harness, which compiles the engine and these fixtures into one assembly:
        /// without it this rule would report its own deliberate violation sample.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            new[] { typeof(SetupRandomnessAuthority).Assembly, typeof(GuardsDigital.Application.MatchSession).Assembly }
                .Distinct()
                .SelectMany(ILConformanceScanner.TypesIn)
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    (type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal) ||
                     type.Namespace.StartsWith("GuardsDigital.Application", StringComparison.Ordinal)))
                .Distinct();

        // -------------------------------------------------------------------------------------------
        // The samples the rule is measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>A setup decision that draws a number nothing records.</summary>
        private static class UnrecordedDrawSample
        {
            public static int Draw()
            {
                using (var random = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[1];
                    random.GetBytes(bytes);
                    return bytes[0];
                }
            }
        }

        /// <summary>The ordinary shape: a decision derived from a seed somebody wrote down.</summary>
        private static class RecordedDerivationSample
        {
            public static ulong Draw(SetupRandomnessSeed seed) =>
                SetupRandomnessAuthority.Derive(seed, SetupRandomnessPurpose.TieBreakerCoinFlip);
        }
    }
}

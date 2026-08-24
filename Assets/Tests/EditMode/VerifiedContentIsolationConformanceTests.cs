using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// No rule branches on which hero it is looking at.
    ///
    /// <para>Five roadmap blocks - milestones 8.20 through 8.24 - each require their hero's behaviour to
    /// work "without an &lt;Hero&gt;-name branch", and until this fixture existed nothing enforced it.
    /// The property held at milestone 8.20 and was verifiable only by reading the code or by grepping
    /// it, which is exactly the shape of claim milestone 6.1k's scanner was built to stop trusting: a
    /// substring search answers a question about spelling, and the rule is about references.</para>
    ///
    /// <para><b>Why it matters more with every hero.</b> A hero-name branch is not a style problem; it
    /// is the failure mode the whole roster re-baseline is arranged to avoid. Arien's Spell Break needed
    /// a restriction that carves out a colour, and the cheap way to ship it would have been to ask
    /// whether the card is Spell Break. That card would then work and Brogan's, Sabina's, Tigerclaw's
    /// and Xargatha's equivalents would each need their own branch - five readings of one rule, able to
    /// disagree. The expensive way is what milestone 8.20 did: a declaration the card fills in and a
    /// query that has never heard of Arien.</para>
    ///
    /// <para><b>What this fixture is not.</b> It does not forbid naming a hero in presentation, in a
    /// display name, or in a comment. Its subject is the rules and application assemblies, where a
    /// hero's identity is content a definition supplies rather than a fact a rule may read.</para>
    /// </summary>
    public sealed class VerifiedContentIsolationConformanceTests
    {
        /// <summary>
        /// The production types that may name verified hero content, and why each may.
        ///
        /// <para>It is a declared list with a stated reason rather than a set of suppressions, in the
        /// shape <c>ConfiguredCardMatrixTests</c>' frame-shape stubs take: every entry is audited by the
        /// third case below, which fails when an entry stops referencing content at all. An exclusion
        /// list nothing reads is a place things go quietly.</para>
        ///
        /// <para>All four are composition rather than resolution. Not one of them asks a question about
        /// a hero and acts on the answer: they say what this build registers, what each seat is dealt,
        /// and which content identity stands at which spawn.</para>
        /// </summary>
        private static readonly (Type Site, string Reason)[] ContentAwareSites =
        {
            (typeof(MatchContentCatalogs),
                "declares what this build registers, which is where the triggered effects a hero's " +
                "Ultimate runs join the one catalog a snapshot compares"),
            (typeof(SyntheticEffectCapabilities),
                "is that catalog's single construction site, so every hero's Active-effect handlers " +
                "are concatenated here or nowhere"),
            (typeof(PlaceholderOneVsOneTestConfiguration),
                "composes the staged match and therefore states which printed set each seat is dealt"),
            (typeof(QuickGameSetupConfiguration),
                "binds a hero content identity to a participant at a configured spawn point, which is " +
                "what addresses that hero's prepared token")
        };

        /// <summary>
        /// Milestone 8.20's rule, asked of compiled references rather than of source text: no rule may
        /// reach into a verified hero's content.
        /// </summary>
        [Test]
        public void NoRule_ReferencesAVerifiedHerosContent()
        {
            var violations = ILConformanceScanner.FindForbiddenReferences(
                AuditedTypes(),
                VerifiedContentMembers(),
                "reads verified hero content, so a rule can differ by which hero it is looking at");

            Assert.That(violations, Is.Empty,
                "A rule must not reference a hero's printed content. These do:\n" +
                string.Join("\n", violations) +
                "\nIf the reference is composition rather than resolution - registering content, " +
                "dealing a hand, or binding an identity to a seat - add the type to " +
                nameof(ContentAwareSites) + " with the reason it belongs there.");
        }

        /// <summary>
        /// The same rule against the evasion the first case cannot see. A type that spells
        /// <c>"arien-spell-break"</c> as a literal never references <see cref="ArienCardContent"/> and
        /// would pass above while doing precisely what the rule forbids.
        /// </summary>
        [Test]
        public void NoRule_SpellsAVerifiedHerosIdentityAsAStringLiteral()
        {
            var forbidden = ForbiddenLiterals();
            Assert.That(forbidden, Is.Not.Empty, "or this case holds over nothing.");

            var violations = ILConformanceScanner.Scan(AuditedTypes())
                .SelectMany(method => method.Instructions
                    .Where(instruction => instruction.Text != null)
                    .Where(instruction => forbidden.Contains(instruction.Text))
                    .Select(instruction => method.Description + ": spells '" + instruction.Text + "'"))
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(violations, Is.Empty,
                "A rule must not spell a hero's content-set identity, card identity or content " +
                "revision. These do:\n" + string.Join("\n", violations));
        }

        /// <summary>
        /// The exclusion list is audited rather than trusted. An entry that stops referencing content -
        /// because the site was refactored, renamed, or is simply no longer needed - fails here instead
        /// of remaining as a standing permission nobody reads.
        /// </summary>
        [Test]
        public void EveryDeclaredContentAwareSite_StillReferencesVerifiedContent()
        {
            var members = VerifiedContentMembers().ToArray();
            var literals = ForbiddenLiterals();

            foreach (var (site, reason) in ContentAwareSites)
            {
                var referencesMembers = ILConformanceScanner
                    .FindForbiddenReferences(new[] { site }, members, "reads content").Count > 0;
                var referencesLiterals = ILConformanceScanner.Scan(site)
                    .SelectMany(method => method.Instructions)
                    .Any(instruction => instruction.Text != null && literals.Contains(instruction.Text));

                Assert.That(referencesMembers || referencesLiterals, Is.True,
                    site.FullName + " is declared content-aware because it " + reason +
                    ", but it no longer names any verified content. Remove it from " +
                    nameof(ContentAwareSites) + " so the permission does not outlive its reason.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Subjects
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every verified hero content class this build ships. Discovered from the namespace rather
        /// than listed, so the hero added at milestone 8.21 is audited by existing rather than by
        /// somebody remembering to extend a list here.
        /// </summary>
        private static IReadOnlyList<Type> VerifiedContentTypes()
        {
            var types = ILConformanceScanner.TypesIn(typeof(WaspCardContent).Assembly)
                .Where(type => string.Equals(
                    type.Namespace, typeof(WaspCardContent).Namespace, StringComparison.Ordinal))
                .ToArray();

            Assert.That(types, Is.Not.Empty,
                "The verified content namespace is empty, so every rule below would hold vacuously.");
            return types;
        }

        private static IEnumerable<MemberInfo> VerifiedContentMembers() =>
            VerifiedContentTypes()
                .SelectMany(type => type.GetMembers(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .ToArray();

        /// <summary>
        /// The identities a rule must not write down: each hero's content-set name, every printed card
        /// identity, and every content revision. Read off the content itself, so a renamed card moves
        /// the rule with it.
        /// </summary>
        private static HashSet<string> ForbiddenLiterals()
        {
            var literals = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in VerifiedContentTypes())
            {
                var contentSetId = type.GetField("ContentSetId", BindingFlags.Public | BindingFlags.Static);
                if (contentSetId != null && contentSetId.GetValue(null) is string setId)
                {
                    literals.Add(setId);
                }

                var revision = type.GetField("ContentRevision", BindingFlags.Public | BindingFlags.Static);
                if (revision != null && revision.GetValue(null) is string revisionValue)
                {
                    literals.Add(revisionValue);
                }

                var all = type.GetProperty("AllDefinitions", BindingFlags.Public | BindingFlags.Static);
                if (all?.GetValue(null) is IEnumerable<CardDefinition> definitions)
                {
                    foreach (var definition in definitions)
                    {
                        literals.Add(definition.Id.Value);
                    }
                }
            }

            return literals;
        }

        /// <summary>
        /// Every production rules and application type except the declared content-aware sites.
        ///
        /// <para>The namespace filter is not decoration. The cross-platform harness compiles Core,
        /// Application and these fixtures into one assembly, and without it this rule would be handed
        /// its own subjects - this class names verified content on purpose - and fail against itself.
        /// <c>EffectSeamConformanceTests</c> records the same trap.</para>
        ///
        /// <para>Presentation is deliberately out of scope. A hero's name is a thing presentation
        /// legitimately draws, so a blanket ban there would forbid the correct behaviour rather than
        /// the incorrect one; what this rule is about is a hero's identity reaching a decision.</para>
        /// </summary>
        private static IReadOnlyList<Type> AuditedTypes()
        {
            var contentNamespace = typeof(WaspCardContent).Namespace ?? string.Empty;
            var excluded = ContentAwareSites.Select(entry => entry.Site).ToArray();

            return ILConformanceScanner.TypesIn(typeof(MatchContentCatalogs).Assembly)
                .Concat(ILConformanceScanner.TypesIn(typeof(CardInstance).Assembly))
                .Concat(ILConformanceScanner.TypesIn(typeof(MatchSession).Assembly))
                .Distinct()
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital", StringComparison.Ordinal) &&
                    !(type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Tests", StringComparison.Ordinal))
                .Where(type => !string.Equals(
                    type.Namespace, contentNamespace, StringComparison.Ordinal))
                .Where(type => !excluded.Contains(DeclaringRoot(type)))
                .ToArray();
        }

        /// <summary>
        /// The outermost declaring type, so a nested type or a compiler-generated closure inside a
        /// declared site is covered by that site's permission rather than reported as a violation of
        /// its own.
        /// </summary>
        private static Type DeclaringRoot(Type type)
        {
            var current = type;
            while (current.DeclaringType != null)
            {
                current = current.DeclaringType;
            }

            return current;
        }
    }
}

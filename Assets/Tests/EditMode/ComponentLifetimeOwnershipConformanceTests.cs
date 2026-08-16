using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2 acceptance check 4: token and marker lifetime is not inferred from art or UI
    /// objects.
    ///
    /// Stated as a compiled-member rule rather than a source or naming rule, for the reason milestones
    /// 6.1ap and 6.2a recorded: the question is what code actually references, and a fully-qualified or
    /// aliased call defeats every textual token. What the rule forbids is the whole lifetime vocabulary -
    /// creating a token or marker instance, putting one on the board, taking one back, giving one, and
    /// returning one. A presentation type that cannot reach any of those cannot be where a component
    /// begins or ends, whatever it draws.
    ///
    /// The negative rule alone would be satisfied by an engine in which nothing places tokens at all, so
    /// the positive rule below names where that work does live.
    /// </summary>
    public sealed class ComponentLifetimeOwnershipConformanceTests
    {
        [Test]
        public void NoPresentationType_CanBeginOrEndAComponentLifetime()
        {
            var presentation = ILConformanceScanner.TypesIn(typeof(PlaceholderObstacleView).Assembly);

            Assert.That(presentation, Is.Not.Empty,
                "The presentation assembly produced no types, so this rule would pass vacuously.");
            Assert.That(ILConformanceScanner.UnresolvedTokens(presentation), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");
            Assert.That(
                FindLifetimeViolations(presentation),
                Is.Empty,
                "A presentation type reaches a token or marker lifetime operation. Components are " +
                "authoritative state; a view may one day render them, but it may never decide that one " +
                "exists or has ended.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindLifetimeViolations(new[] { typeof(TokenCreatingSample) }),
                Is.Not.Empty,
                "A type that constructs a placed token must be reported, or the rule is inert.");

            Assert.That(
                FindLifetimeViolations(new[] { typeof(TokenReadingSample) }),
                Is.Empty,
                "A type that only reads where tokens are must not be reported, or the rule bars the " +
                "rendering a later presentation milestone is expected to do.");
        }

        /// <summary>
        /// The rule's other half. Component lifetime has to happen somewhere, and where it happens is
        /// the registered capability handlers and the shared rules operations they invoke - the same
        /// seam every other rules mutation goes through.
        ///
        /// <see cref="PlaceOperation"/> joined that set at milestone 6.5, when the token placement
        /// capability stopped implementing p.18's Place a second time and became one caller of the one
        /// shared operation. The rule's subject is unchanged: what it forbids is a presentation type
        /// deciding that a component exists, and what it requires is that some Core rules seam still
        /// does. Naming only the handler after the commit moved would have made the rule pass
        /// vacuously while nothing was checked.
        /// </summary>
        [Test]
        public void TheCapabilityHandlers_AreWhereComponentLifetimeActuallyHappens()
        {
            var handlers = ILConformanceScanner
                .WithNestedTypes(typeof(SyntheticComponentCapabilities))
                .Concat(new[] { typeof(PlaceOperation) })
                .ToArray();

            foreach (var member in ForbiddenLifetimeMembers())
            {
                Assert.That(
                    ILConformanceScanner.References(handlers, member),
                    Is.True,
                    $"No registered capability reaches {ILConformanceScanner.Describe(member)}. If the " +
                    "engine stopped placing, reclaiming, giving, or returning components, presentation " +
                    "would pass the rule above while nothing owned the lifetime at all.");
            }
        }

        private static IReadOnlyList<string> FindLifetimeViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                ForbiddenLifetimeMembers(),
                "a token or marker lifetime operation reached from presentation");

        private static IReadOnlyList<MemberInfo> ForbiddenLifetimeMembers()
        {
            var members = new MemberInfo[]
            {
                ILConformanceScanner.Constructor(typeof(PlacedToken)),
                ILConformanceScanner.Constructor(typeof(MarkerAttachment)),
                ILConformanceScanner.Method(
                    typeof(ITokenPlacementAuthority),
                    nameof(ITokenPlacementAuthority.CommitValidatedTokenPlacement)),
                ILConformanceScanner.Method(
                    typeof(ITokenPlacementAuthority),
                    nameof(ITokenPlacementAuthority.CommitValidatedTokenReclamation)),
                ILConformanceScanner.Method(
                    typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Give)),
                ILConformanceScanner.Method(
                    typeof(MarkerAttachmentRegister), nameof(MarkerAttachmentRegister.Return))
            };

            Assert.That(members, Has.Length.EqualTo(6),
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
            return members;
        }

        /// <summary>Deliberate violation: decides that a token exists.</summary>
        private sealed class TokenCreatingSample
        {
            public PlacedToken Create(TokenDefinition definition, GuardsDigital.Core.Board.HexCoordinate at) =>
                new PlacedToken(
                    new TokenInstanceId("sample"), definition, default, default, default, at);
        }

        /// <summary>Compliant twin: reads placed tokens without deciding anything about them.</summary>
        private sealed class TokenReadingSample
        {
            public IReadOnlyList<GuardsDigital.Core.Board.HexCoordinate> Coordinates(
                IReadOnlyList<PlacedToken> tokens)
            {
                var coordinates = new List<GuardsDigital.Core.Board.HexCoordinate>();
                foreach (var token in tokens)
                {
                    coordinates.Add(token.Coordinate);
                }

                return coordinates;
            }
        }
    }
}

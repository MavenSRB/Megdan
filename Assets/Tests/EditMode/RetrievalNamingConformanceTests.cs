using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.13's naming audit, stated as two rules rather than as three corrected spellings.
    ///
    /// A `Synthetic` or `Fixture` qualifier is a claim about reach: it says the thing behind it is bounded
    /// content or a fixture-only entry point rather than a rule the configured match runs. The claim goes
    /// stale silently, because nothing about a name breaks when its last fixture-only caller is replaced by
    /// a production one - which is exactly what happened to end-of-round card retrieval, whose handler,
    /// eligibility question and availability declaration were all named for fixtures while
    /// <see cref="RoundResolutionComposition.ForMatch"/> was the only thing composing them.
    ///
    /// So the rules are written against reach rather than against a spelling. The first says where a
    /// `Synthetic` declaration may live; the second says what a configured match may compose. Neither
    /// forbids the qualifier - milestone 6.13's own acceptance check permits keeping one where a precise
    /// limitation remains, which is why <c>SyntheticRepeatedRound</c> keeps its name - and both carry a
    /// vacuity guard, because a rule with nothing to inspect is not evidence.
    /// </summary>
    public sealed class RetrievalNamingConformanceTests
    {
        /// <summary>
        /// The qualifiers that claim limited reach. `Placeholder` is included because the 1v1 test
        /// setup a match is still composed from is named for one, and a stage collaborator must never
        /// be mistaken for it.
        /// </summary>
        private static readonly string[] LimitedReachQualifiers = { "Synthetic", "Fixture", "Placeholder" };

        private const string SyntheticContentNamespaceSuffix = ".Synthetic";

        // ---------------------------------------------------------------------------------------
        // Rule 1: a Synthetic-qualified declaration lives in a declared synthetic-content area
        // ---------------------------------------------------------------------------------------

        [Test]
        public void EverySyntheticQualifiedProductionType_IsDeclaredInASyntheticContentNamespace()
        {
            var qualified = ProductionTypes()
                .Where(type => type.Name.IndexOf("Synthetic", StringComparison.Ordinal) >= 0)
                .ToArray();

            Assert.That(qualified.Length, Is.GreaterThan(4),
                "Too few Synthetic-qualified production types were found for this rule to be inspecting anything.");
            Assert.That(
                qualified.Any(type => IsInSyntheticContentArea(type)), Is.True,
                "No declared synthetic-content namespace was found, so the rule could not distinguish one.");

            var misplaced = qualified
                .Where(type => !IsInSyntheticContentArea(type))
                .Select(type => (type.Namespace ?? "<global>") + "." + type.Name)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(misplaced, Is.Empty,
                "A Synthetic-qualified type outside a '" + SyntheticContentNamespaceSuffix +
                "' namespace claims to be bounded content while living among the general rules: " +
                string.Join(", ", misplaced));
        }

        [Test]
        public void TheContentAreaRule_AdmitsADeclaredAreaAndRefusesAGeneralRuleNamespace()
        {
            // The rule's own allowed and forbidden samples, so it cannot pass by inspecting nothing.
            Assert.That(IsSyntheticContentNamespace("GuardsDigital.Core.Effects.Synthetic"), Is.True);
            Assert.That(IsSyntheticContentNamespace("GuardsDigital.Core.Components.Synthetic"), Is.True);
            Assert.That(IsSyntheticContentNamespace("GuardsDigital.Core.Cards"), Is.False);
            Assert.That(IsSyntheticContentNamespace("GuardsDigital.Core.SyntheticCards"), Is.False,
                "A namespace that merely mentions the word is not a declared content area.");
            Assert.That(IsSyntheticContentNamespace(null), Is.False);
        }

        // ---------------------------------------------------------------------------------------
        // Rule 2: what a configured match composes carries no limited-reach qualifier
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheEndOfRoundCollaborators_AConfiguredMatchComposes_CarryNoLimitedReachQualifier()
        {
            // A live session rather than a bare aggregate: the aggregate composes its graphs only when a
            // session rebuilds its initial state, and it is the session's composition this rule is about.
            var session = new MatchSession(CreateConfiguredInitialState());
            var round = ((MatchAggregate)Field(session, "aggregate")).Round;
            Assert.That(round, Is.Not.Null, "A started session must hold a round authority to inspect.");
            var availability = Field(round, "endOfRoundStageAvailability");
            var handlers = (IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler>)
                Field(round, "endOfRoundHandlers");

            // Vacuity guards: this must be the configured declaration, holding a real retrieval handler.
            // The declaration is asked rather than cast, so the rule survives the rename it closes.
            Assert.That(availability, Is.Not.Null);
            Assert.That(
                Admits(availability, EndOfRoundStage.RemoveTokensAndReturnMarkers), Is.True,
                "A configured match admits the cleanup stage; without it this is not the configured declaration.");
            Assert.That(handlers.ContainsKey(EndOfRoundStage.RetrieveCardsAndCancelActiveEffects), Is.True,
                "The configured match must have composed a retrieval handler for this rule to inspect one.");

            var composed = new List<Type> { availability.GetType() };
            composed.AddRange(handlers.Values.Select(handler => handler.GetType()));

            var offenders = composed
                .Where(type => Qualifier(type.Name) != null)
                .Select(type => type.Name + " (" + Qualifier(type.Name) + ")")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "A configured production match composes these, so a limited-reach qualifier on one is false: " +
                string.Join(", ", offenders));
        }

        [Test]
        public void TheCardLifecycleRetrievalEntryPoint_CarriesNoLimitedReachQualifier()
        {
            var retrieval = typeof(CardLifecycleAuthority)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name.IndexOf("Retriev", StringComparison.Ordinal) >= 0)
                .ToArray();

            Assert.That(retrieval.Length, Is.EqualTo(1),
                "The audit forbids a second eligibility query, so exactly one retrieval entry point may exist.");

            var offender = Qualifier(retrieval[0].Name);
            Assert.That(offender, Is.Null,
                "The end-of-round stage a configured match runs calls " + retrieval[0].Name +
                ", so its '" + offender + "' qualifier is false.");
        }

        [Test]
        public void TheQualifierRule_NamesAViolatorAndAcceptsItsCompliantTwin()
        {
            // The three names this milestone corrected are the forbidden samples, kept as literals so the
            // rule keeps stating what it rejects after the declarations themselves stopped saying it.
            Assert.That(Qualifier("Synthetic" + "CardRetrievalHandler"), Is.EqualTo("Synthetic"));
            Assert.That(Qualifier("EndOfRound" + "FixtureCapabilities"), Is.EqualTo("Fixture"));
            Assert.That(Qualifier("TryRetrieveEligible" + "SyntheticCard"), Is.EqualTo("Synthetic"));
            Assert.That(Qualifier("PlaceholderOneVsOneTestConfiguration"), Is.EqualTo("Placeholder"),
                "A placeholder setup keeps its qualifier; the rule is about what a match composes, not about it.");

            // Their compliant twins, which are the names in the source today.
            Assert.That(Qualifier(nameof(CardRetrievalHandler)), Is.Null);
            Assert.That(Qualifier(nameof(EndOfRoundStageAvailability)), Is.Null);
            Assert.That(Qualifier(nameof(CardLifecycleAuthority.TryRetrieveEligibleCard)), Is.Null);
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>The qualifier a name claims limited reach with, or <c>null</c> if it claims none.</summary>
        private static string Qualifier(string name) =>
            LimitedReachQualifiers.FirstOrDefault(value => name.IndexOf(value, StringComparison.Ordinal) >= 0);

        private static bool IsInSyntheticContentArea(Type type) => IsSyntheticContentNamespace(type.Namespace);

        private static bool IsSyntheticContentNamespace(string namespaceName) =>
            namespaceName != null &&
            namespaceName.EndsWith(SyntheticContentNamespaceSuffix, StringComparison.Ordinal);

        /// <summary>
        /// Every non-nested production type. Filtering by namespace rather than by assembly is deliberate:
        /// the cross-platform harness compiles Core, Application and the tests into one assembly, so an
        /// assembly filter would drag every fixture in on that tier and none of them on Unity's.
        /// </summary>
        private static IEnumerable<Type> ProductionTypes() =>
            new[] { typeof(RoundResolutionAuthority).Assembly, typeof(MatchAggregate).Assembly }
                .Distinct()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsNested)
                .Where(type => type.Namespace != null &&
                    (type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal) ||
                        type.Namespace.StartsWith("GuardsDigital.Application", StringComparison.Ordinal)));

        private static bool Admits(object availability, EndOfRoundStage stage)
        {
            var method = availability.GetType().GetMethod("IsAvailable");
            Assert.That(method, Is.Not.Null,
                "The end-of-round availability declaration no longer answers IsAvailable.");
            return (bool)method.Invoke(availability, new object[] { stage });
        }

        private static object Field(object instance, string name)
        {
            var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                instance.GetType().Name + " no longer declares a '" + name + "' field.");
            return field.GetValue(instance);
        }

        /// <summary>The ordinary configured match on the official map, built the way the aggregate builds it.</summary>
        private static MatchSessionInitialState CreateConfiguredInitialState()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new BoardMapDefinition[] { map });
        }
    }
}

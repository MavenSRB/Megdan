using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.10's first and second acceptance checks, stated as rules rather than as sampled
    /// cases - the first of them inverted at milestone 6.11, which is the milestone it was waiting for.
    ///
    /// The first rule was "nothing in the engine reaches p.8's ordered prefix", because the page's third
    /// step did not exist and a match that ran the first two would have moved its Battle Zone away from
    /// minions that were never removed and never respawned. Milestone 6.11 implements the third step, so
    /// the rule turns over rather than being deleted: p.8's stages may be run by the Lane-Push step
    /// rules and by nothing else. That is the same invariant from the other side - a caller that flipped
    /// a Wave counter or advanced the Zone on its own would be running a push no continuation was
    /// recording, which is the partial transition in a new disguise.
    ///
    /// The second: the authoritative <c>CurrentBattleZoneId</c> is what the shared Battle-Zone query
    /// reads. That is held from the other end - nothing but Quick Game setup and the board's own
    /// construction may read the map's starting-Battle-Zone flag - because a rules question that
    /// reached the flag would answer about the Zone the match began in for the rest of the match, and
    /// would do it silently.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class LanePushPrefixIsolationConformanceTests
    {
        // -------------------------------------------------------------------------------------------
        // Rule 1: p.8's stages are run by the transition's step rules and by nothing else
        // -------------------------------------------------------------------------------------------

        [Test]
        public void NothingButTheStepRules_RunsAPageEightStage()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindStageRunners(subjects), Is.Empty,
                "Something outside the Lane-Push step rules runs one of p.8's stages. A Wave counter " +
                "flipped or a Zone advanced outside the transition is a push no continuation is " +
                "recording, so a snapshot taken across it would restore a match that had skipped a " +
                "step somebody still owed.");
        }

        [Test]
        public void BothLanePushRequests_NowReachTheTransition()
        {
            // The vacuity guard for the rule above. An engine in which nothing consumed a Lane-Push
            // request at all would satisfy "nothing but the step rules runs a stage" perfectly, so the
            // two consumers are named as the thing that is supposed to be reaching them.
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(LanePushStepRules)),
                    ILConformanceScanner.Method(
                        typeof(LanePushTransitionAuthority),
                        nameof(LanePushTransitionAuthority.ResolveWaveStage))),
                Is.True,
                "The step rules are what run p.8's stages; a rule permitting only them is worth " +
                "nothing if they no longer do.");
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(RoundResolutionAuthority)),
                    ILConformanceScanner.Method(
                        typeof(LanePushConsumption), nameof(LanePushConsumption.TryConsume),
                        typeof(LanePushRequestId), typeof(ZoneId), typeof(TeamSide),
                        typeof(TeamSide), typeof(TieBreakerState), typeof(bool))),
                Is.True,
                "The end-of-round request reaches the transition at its consumption stage.");
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(TurnResolutionAuthority)),
                    ILConformanceScanner.Method(
                        typeof(LanePushConsumption), nameof(LanePushConsumption.TryConsume),
                        typeof(LanePushRequestId), typeof(ZoneId), typeof(TeamSide),
                        typeof(TeamSide), typeof(TieBreakerState), typeof(bool))),
                Is.True,
                "And the mid-turn request reaches it at the Initiative advance p.8 puts it before.");
        }

        [Test]
        public void NothingButTheLanePushTransition_MovesTheBattleZone()
        {
            // The companion to the rule above, from the other side of the seam. Keeping the prefix
            // unreachable is worth little if the advance it performs is reachable on its own: a caller
            // that moved the Battle Zone directly would skip p.8's Wave stage entirely, and with it the
            // final-Wave victory the page checks before the Zone moves at all.
            Assert.That(FindZoneMovers(AuditedTypes().ToArray()), Is.Empty,
                "Something outside the Lane-Push transition advances p.8's Battle Zone.");

            Assert.That(FindZoneMovers(new[] { typeof(DirectZoneAdvanceSample) }), Is.Not.Empty,
                "A direct advance must be reported, or the rule is inert.");
            Assert.That(FindZoneMovers(new[] { typeof(ZoneReadingSample) }), Is.Empty,
                "Reading the Zone is not moving it, or the rule bans every consumer it exists to serve.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindStageRunners(new[] { typeof(DirectStageSample) }), Is.Not.Empty,
                "A caller that flips a Wave counter outside the transition must be reported, or the " +
                "rule is inert.");

            Assert.That(FindStageRunners(new[] { typeof(RequestConsumingSample) }), Is.Empty,
                "The compliant twin hands the request to the transition and must not be reported, or " +
                "the rule bans the shape that is now correct.");
        }

        [Test]
        public void ThePushSeam_CannotBeHandedALiveRequest()
        {
            var parameters = ILConformanceScanner
                .Method(
                    typeof(LanePushTransitionAuthority),
                    nameof(LanePushTransitionAuthority.TryBeginPush))
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            // Membership is asserted as a boolean rather than through a collection constraint on
            // purpose: Unity's NUnit binds `Does.Not.Contain` on a sequence of Type to the string
            // overload and refuses to compile it, which a green cross-platform harness never reveals.
            Assert.That(
                parameters.Any(parameter => parameter == typeof(PendingImmediateLanePushRequest)),
                Is.False);
            Assert.That(
                parameters.Any(parameter => parameter == typeof(PendingLanePushRequest)), Is.False,
                "p.8 decides the winner and the loser before the transition begins, so the seam names " +
                "two sides. A seam that took a request would be one assignment away from consuming one.");
        }

        // -------------------------------------------------------------------------------------------
        // Rule 2: only setup and the board's construction read the map's flag
        // -------------------------------------------------------------------------------------------

        [Test]
        public void NoRulesQuestion_ReadsTheStartingBattleZoneFlag()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(FindFlagReaders(subjects), Is.Empty,
                "A Battle-Zone question is answered from the board's authoritative current Zone. " +
                "Reading the map's flag instead is not a compile error and not a visibly wrong " +
                "answer - it is the right answer until the Lane is first pushed, and the wrong one " +
                "for the rest of the match.");
        }

        [Test]
        public void ThePermittedReaders_DoReadIt()
        {
            Assert.That(ReadsTheFlag(typeof(QuickGameSetupAuthority)), Is.True,
                "Setup still requires configured minions to be placed in the Zone the match starts in.");
            Assert.That(ReadsTheFlag(typeof(AdjacentMoveAuthority)), Is.True,
                "And the board still opens its current Zone at the flagged one. A rule forbidding " +
                "every reader is satisfied by an engine with no readers at all.");
        }

        [Test]
        public void FlagRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindFlagReaders(new[] { typeof(StaleZoneReadingSample) }), Is.Not.Empty);
            Assert.That(FindFlagReaders(new[] { typeof(CurrentZoneReadingSample) }), Is.Empty);
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(StageForms(), Is.Not.Empty);
            Assert.That(FlagForms(), Is.Not.Empty,
                "A rule that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rules
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindStageRunners(IEnumerable<Type> types)
        {
            var forbidden = StageForms();
            return types
                .Where(type => !IsPermittedStageRunner(type))
                .SelectMany(type => forbidden
                    .Where(member => ILConformanceScanner.References(new[] { type }, member))
                    .Select(member => $"{type.FullName} runs {ILConformanceScanner.Describe(member)}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// The step rules, which are what a transition step is, and the authority that declares the
        /// stages - the rule is about who else may run one, not about their existing.
        /// </summary>
        private static bool IsPermittedStageRunner(Type type) =>
            type == typeof(LanePushStepRules) ||
            type == typeof(LanePushTransitionAuthority) ||
            type.DeclaringType == typeof(LanePushStepRules) ||
            type.DeclaringType == typeof(LanePushTransitionAuthority);

        private static IReadOnlyList<MethodInfo> StageForms() => new[]
        {
            ILConformanceScanner.Method(
                typeof(LanePushTransitionAuthority),
                nameof(LanePushTransitionAuthority.ResolveWaveStage)),
            ILConformanceScanner.Method(
                typeof(LanePushTransitionAuthority),
                nameof(LanePushTransitionAuthority.ResolveZoneStage)),
            ILConformanceScanner.Method(
                typeof(LanePushTransitionAuthority),
                nameof(LanePushTransitionAuthority.ResolvePreviousZoneRemoval))
        };

        private static IReadOnlyList<string> FindZoneMovers(IEnumerable<Type> types)
        {
            var forbidden = ILConformanceScanner.Method(
                typeof(AdjacentMoveAuthority),
                nameof(AdjacentMoveAuthority.CommitValidatedBattleZoneAdvance));
            return types
                .Where(type => type != typeof(LanePushTransitionAuthority) &&
                    type != typeof(AdjacentMoveAuthority))
                .Where(type => ILConformanceScanner.References(new[] { type }, forbidden))
                .Select(type => $"{type.FullName} advances p.8's Battle Zone")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<string> FindFlagReaders(IEnumerable<Type> types)
        {
            var forbidden = FlagForms();
            return types
                .Where(type => !IsPermittedFlagReader(type))
                .SelectMany(type => forbidden
                    .Where(member => ILConformanceScanner.References(new[] { type }, member))
                    .Select(member => $"{type.FullName} reads {ILConformanceScanner.Describe(member)}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Setup, which asks a setup question, and the board, which derives its opening value. The
        /// query that owns both readings is excluded because the rule is about who asks the stale
        /// question, not about the question existing.
        /// </summary>
        private static bool IsPermittedFlagReader(Type type) =>
            type == typeof(QuickGameSetupAuthority) ||
            type == typeof(AdjacentMoveAuthority) ||
            type == typeof(BattleZoneQuery) ||
            type.DeclaringType == typeof(QuickGameSetupAuthority) ||
            type.DeclaringType == typeof(AdjacentMoveAuthority);

        private static IReadOnlyList<MethodInfo> FlagForms() => new[]
        {
            ILConformanceScanner.Method(
                typeof(BattleZoneQuery), nameof(BattleZoneQuery.GetStartingBattleZones)),
            ILConformanceScanner.Method(
                typeof(BattleZoneQuery), nameof(BattleZoneQuery.TryGetStartingBattleZone)),
            ILConformanceScanner.Method(
                typeof(BattleZoneQuery), nameof(BattleZoneQuery.IsInStartingBattleZone),
                typeof(BoardMapDefinition), typeof(SpaceId)),
            ILConformanceScanner.Method(
                typeof(BattleZoneQuery), nameof(BattleZoneQuery.IsInStartingBattleZone),
                typeof(BoardMapDefinition), typeof(HexCoordinate)),
            ILConformanceScanner.Method(
                typeof(BattleZoneQuery), nameof(BattleZoneQuery.FindStartingBattleZoneCoordinates))
        };

        private static bool ReadsTheFlag(Type type) =>
            FlagForms().Any(member => ILConformanceScanner.References(new[] { type }, member));

        /// <summary>
        /// Every Core type, so a rule written after this milestone is audited by existing rather than
        /// by being added to a list. Compiler-generated closures are skipped for the reason every scan
        /// here skips them: they carry the enclosing method's instructions, which are already scanned.
        /// The namespace filter matters in the cross-platform harness, which compiles Core and these
        /// fixtures into one assembly, so without it these rules would report their own deliberate
        /// violation samples.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(BattleZoneQuery).Assembly)
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal));

        // -------------------------------------------------------------------------------------------
        // The samples the rules are measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A caller that runs one of p.8's stages itself. It compiles, it flips a real Wave counter,
        /// and no continuation anywhere records that it happened.
        /// </summary>
        private static class DirectStageSample
        {
            public static LanePushStageOutcome Flip(LanePushTransitionAuthority transition) =>
                transition.ResolveWaveStage();
        }

        /// <summary>The shape that is correct: hand the request to the transition and let it run.</summary>
        private static class RequestConsumingSample
        {
            public static LanePushConsumptionResult Consume(
                PendingImmediateLanePushRequest request, LanePushConsumption consumption) =>
                consumption.TryConsume(
                    request.RequestId, request.WinningSide, request.LosingSide,
                    new TieBreakerState(TeamSide.Red));
        }

        /// <summary>A caller that moves the Battle Zone without the Wave stage that precedes it.</summary>
        private static class DirectZoneAdvanceSample
        {
            public static bool Advance(AdjacentMoveAuthority board, ZoneId destination) =>
                board.CommitValidatedBattleZoneAdvance(destination);
        }

        /// <summary>The ordinary shape: read the Zone and answer a question about it.</summary>
        private static class ZoneReadingSample
        {
            public static bool CountsInTheZone(AdjacentMoveAuthority board, SpaceId spaceId) =>
                BattleZoneQuery.IsInCurrentBattleZone(board, spaceId);
        }

        /// <summary>A rules question answered from the map's flag rather than from the board.</summary>
        private static class StaleZoneReadingSample
        {
            public static bool CountsForTheMinionBattle(
                IBoardDisplacementAuthority board, SpaceId spaceId) =>
                BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, spaceId);
        }

        /// <summary>The same question, asked of the Zone the Lane has reached.</summary>
        private static class CurrentZoneReadingSample
        {
            public static bool CountsForTheMinionBattle(
                IBoardDisplacementAuthority board, SpaceId spaceId) =>
                BattleZoneQuery.IsInCurrentBattleZone(board, spaceId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8f acceptance check 2: no rules-consuming entry point declares an implicit
    /// no-modifications default.
    ///
    /// The hazard this rule closes is recorded twice in the roadmap: milestone 6.8's review noted the
    /// optional <c>RuleModificationSet modifications = null</c> parameter loses rules silently when a
    /// caller forgets it, and that is exactly how the Move-Through permission reached a configured
    /// card with no consumer. 6.8c then added further optional parameters rather than closing the
    /// hazard. A required parameter makes the compiler the enforcement; the named no-modification
    /// value is <see cref="RuleModificationSet.Empty"/>.
    ///
    /// This rule is reflection- rather than IL-based - a parameter default lives in metadata, not in
    /// a method body - and it is the repo's first use of <see cref="ParameterInfo.HasDefaultValue"/>.
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    ///
    /// Milestone 6.8i widened it. 6.8f closed the hazard for one parameter type and flagged four
    /// survivors of the same class, optional parameters of other rule-affecting types whose omission
    /// also changes a rules answer in silence: the Tie Breaker state that orders p.17's simultaneous
    /// modifications, and the <see cref="RuleObject"/> actor whose omission disables p.18's "You are
    /// never immune to your own actions" - the exact clause milestone 6.4's Push immunity consumer
    /// depends on. The widened rule governs the modification query surface, which is where a
    /// rule-affecting value is a rules answer rather than incidental data.
    /// </summary>
    public sealed class ModificationSeamConformanceTests
    {
        /// <summary>
        /// The parameter types whose omission silently changes a rules answer rather than merely
        /// leaving a value unset: the live set itself, the ordering state p.17 resolves ties with, and
        /// the object a query is asked about. Each has a named absent value a call site writes when it
        /// genuinely means none - <see cref="RuleModificationSet.Empty"/>,
        /// <see cref="TieBreakerState.Unrevealed"/>, <see cref="RuleObject.None"/> - so requiring them
        /// costs a caller nothing but the statement of what it meant.
        /// </summary>
        private static readonly Type[] RuleAffectingTypes =
        {
            typeof(RuleModificationSet),
            typeof(TieBreakerState),
            typeof(RuleObject)
        };

        [Test]
        public void NoRulesConsumingSeam_DeclaresAnOptionalModificationSet()
        {
            Assert.That(
                FindOptionalParameters(ProductionTypes(), typeof(RuleModificationSet)),
                Is.Empty,
                "A seam whose outcome a modification can change declares an implicit default, so a " +
                "caller that forgets the live set silently resolves against no modifications. " +
                "Passing none must be an explicit, named decision: RuleModificationSet.Empty.");
        }

        [Test]
        public void NoModificationQuerySeam_DeclaresAnOptionalRuleAffectingParameter()
        {
            Assert.That(
                FindOptionalParameters(ModificationSurfaceTypes(), RuleAffectingTypes),
                Is.Empty,
                "A modification-query seam declares an optional parameter of a rule-affecting type, " +
                "so a caller that omits it gets a different rules answer and no diagnostic. An " +
                "absent ordering state, actor, or object must be written at the call site as " +
                "Unrevealed, None, or Empty rather than inherited from a parameter default.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindOptionalParameters(new[] { typeof(OptionalSetSample) }, typeof(RuleModificationSet)),
                Is.Not.Empty,
                "A method with an optional modification-set parameter must be reported, or the rule " +
                "is inert.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(ExplicitSetSample) }, typeof(RuleModificationSet)),
                Is.Empty,
                "A method that requires its modification set must not be reported, or the rule bans " +
                "the shape it exists to enforce.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(OptionalRuleAffectingSample) }, RuleAffectingTypes),
                Is.Not.Empty,
                "A method with an optional Tie Breaker or rules-object parameter must be reported, " +
                "or the widened rule is inert.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(ExplicitRuleAffectingSample) }, RuleAffectingTypes),
                Is.Empty,
                "A method that requires them must not be reported, or the widened rule bans the " +
                "shape it exists to enforce.");
        }

        /// <summary>
        /// The negative rules are satisfied by an engine with no such parameters at all, so both scans
        /// must be shown to see the real seams they govern.
        /// </summary>
        [Test]
        public void TheScan_SeesTheRealModificationSeams()
        {
            Assert.That(
                CountParameters(ProductionTypes(), typeof(RuleModificationSet)),
                Is.GreaterThanOrEqualTo(30),
                "The scan found fewer modification-set parameters than the production surface is " +
                "known to declare, so the subject set or the parameter-type match has silently " +
                "narrowed and the rule could pass vacuously.");

            Assert.That(
                CountParameters(ModificationSurfaceTypes(), RuleAffectingTypes),
                Is.GreaterThanOrEqualTo(20),
                "The widened scan found fewer rule-affecting parameters than the modification query " +
                "surface is known to declare, so its subject set or type match has narrowed and it " +
                "could pass vacuously.");
        }

        private static IReadOnlyList<string> FindOptionalParameters(
            IEnumerable<Type> types,
            params Type[] parameterTypes) =>
            ScanParameters(types, parameterTypes)
                .Where(entry => entry.Parameter.IsOptional || entry.Parameter.HasDefaultValue)
                .Select(entry => entry.Description)
                .ToArray();

        private static int CountParameters(IEnumerable<Type> types, params Type[] parameterTypes) =>
            ScanParameters(types, parameterTypes).Count();

        private static IEnumerable<(ParameterInfo Parameter, string Description)> ScanParameters(
            IEnumerable<Type> types,
            IReadOnlyList<Type> parameterTypes)
        {
            const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var type in types)
            {
                var members = type.GetMethods(declared)
                    .Cast<MethodBase>()
                    .Concat(type.GetConstructors(declared));
                foreach (var member in members)
                {
                    foreach (var parameter in member.GetParameters())
                    {
                        if (parameterTypes.Contains(parameter.ParameterType))
                        {
                            yield return (parameter,
                                $"{type.FullName}.{member.Name}({parameter.ParameterType.Name} " +
                                $"{parameter.Name})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The modification query surface: the namespace that owns the live set, its derivation, and
        /// the queries that read it. Scoped there rather than across all production types because a
        /// rule-affecting type can legitimately be an optional convenience elsewhere - a setup or
        /// diagnostic API may take a Tie Breaker it does not resolve rules with - while inside this
        /// namespace such a parameter is always part of a rules answer.
        /// </summary>
        private static IReadOnlyList<Type> ModificationSurfaceTypes() =>
            ProductionTypes()
                .Where(type => string.Equals(
                    type.Namespace, typeof(RuleModificationSet).Namespace, StringComparison.Ordinal))
                .ToArray();

        /// <summary>
        /// Every type of the Core and Application production assemblies. The namespace filter keeps
        /// the harness's single merged assembly from sweeping this fixture's own samples into the
        /// real subject set; under Unity the two typeof anchors resolve to the two production
        /// assemblies and the filter is a no-op.
        /// </summary>
        private static IReadOnlyList<Type> ProductionTypes() =>
            ILConformanceScanner.TypesIn(typeof(RuleModificationSet).Assembly)
                .Concat(ILConformanceScanner.TypesIn(typeof(MatchSession).Assembly))
                .Distinct()
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital", StringComparison.Ordinal) &&
                    !(type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Tests", StringComparison.Ordinal))
                .ToArray();

        /// <summary>Deliberate violation: the set is optional, so forgetting it loses rules silently.</summary>
        private sealed class OptionalSetSample
        {
            public bool Evaluate(RuleModificationSet modifications = null) => modifications != null;
        }

        /// <summary>Compliant twin: the set is required; supplying none must be written as Empty.</summary>
        private sealed class ExplicitSetSample
        {
            public bool Evaluate(RuleModificationSet modifications) => modifications != null;
        }

        /// <summary>
        /// Deliberate violation of the widened rule: the ordering state and the acting object are
        /// optional, so omitting either changes the answer with nothing to notice.
        /// </summary>
        private sealed class OptionalRuleAffectingSample
        {
            public bool Evaluate(RuleObject target, TieBreakerState tieBreaker = default,
                RuleObject actor = default) =>
                tieBreaker.IsDefined && !actor.Equals(target);
        }

        /// <summary>Compliant twin: both are required, so an absent one is written as Unrevealed or None.</summary>
        private sealed class ExplicitRuleAffectingSample
        {
            public bool Evaluate(RuleObject target, TieBreakerState tieBreaker, RuleObject actor) =>
                tieBreaker.IsDefined && !actor.Equals(target);
        }

        // -----------------------------------------------------------------------------------------
        // Milestone 8.7: the declarable modifier space has a consumer or a reason, member by member
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.7 acceptance check 1, closing the 7.9a review's F-CARD-03.
        ///
        /// The finding was not that a modifier family was missing but that most of the declarable
        /// space was inert: one of six value channels and one of nine restriction activities had a
        /// production consumer, so a card declaring "+1 Attack" or "cannot Attack" would load, persist,
        /// order correctly, survive a snapshot, and change nothing anyone could see. A rules engine
        /// that silently drops a declared rule is wrong in the one way a match cannot detect.
        ///
        /// The rule below makes that impossible to reintroduce without saying so. It enumerates the
        /// space from the enums themselves - so a member added to any of them fails here until someone
        /// decides what it means - and requires every member to be either consumed, proven by an answer
        /// that moves when it is declared, or excluded by name with its reason, which is the
        /// FrameShapeStubs precedent from ConfiguredCardMatrixTests: an exclusion list, not an escape
        /// hatch. An excluded member may not also carry a probe, so nothing can be filed under "no rule
        /// reads this" while quietly working.
        ///
        /// The probes read production seams rather than the modification set. That distinction is the
        /// whole point: <see cref="RuleModificationSet"/> answers every member by construction - it is
        /// a lookup - and what F-CARD-03 was about is whether any rule asks. Where the seam that asks
        /// needs a whole match in flight, the probe reads the narrowest production query in that chain
        /// and <see cref="TheConsumingSeams_AreTheOnesTheEngineActuallyUses"/> supplies the second
        /// link, which is the two-link shape <c>UnitKindQueryConformanceTests</c> already uses.
        /// </summary>
        [Test]
        public void EveryDeclarableModification_HasAProductionConsumerOrANamedExclusion()
        {
            var fixture = SeamFixture.Create();
            var consumed = ConsumptionCases(fixture);
            var declarable = DeclarableMembers();

            var namedTwice = consumed.Select(entry => entry.Member)
                .Intersect(ExcludedMembers.Keys, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(namedTwice, Is.Empty,
                "A member is both consumed and excluded. The exclusion list records what no rule " +
                $"reads; a member with a working consumer does not belong on it: {Join(namedTwice)}");

            var undecided = declarable
                .Where(member => consumed.All(entry => entry.Member != member) &&
                    !ExcludedMembers.ContainsKey(member))
                .ToArray();
            Assert.That(undecided, Is.Empty,
                "A declarable modification has neither a production consumer nor a recorded reason " +
                "for having none. Declaring one would load, persist, and change nothing, which is " +
                $"the F-CARD-03 defect: {Join(undecided)}");

            var unknown = consumed.Select(entry => entry.Member)
                .Concat(ExcludedMembers.Keys)
                .Where(member => !declarable.Contains(member, StringComparer.Ordinal))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(unknown, Is.Empty,
                "A disposition names something that is not a declarable member, so a rename has left " +
                $"the tables pointing at nothing: {Join(unknown)}");

            var unexplained = ExcludedMembers
                .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
                .Select(entry => entry.Key)
                .ToArray();
            Assert.That(unexplained, Is.Empty,
                "A member is excluded without a reason, which is the escape hatch this rule exists " +
                $"to refuse: {Join(unexplained)}");

            var inert = consumed
                .Where(entry => string.Equals(
                    entry.Read(entry.Declared), entry.Read(RuleModificationSet.Empty),
                    StringComparison.Ordinal))
                .Select(entry => $"{entry.Member} at {entry.Seam}")
                .ToArray();
            Assert.That(inert, Is.Empty,
                "A member is recorded as consumed, but declaring it changes nothing its named " +
                "production seam answers. Either the consumer regressed or the disposition is " +
                $"aspirational: {Join(inert)}");
        }

        /// <summary>
        /// Milestone 8.25e7 closes F-TEST-09's remaining finite vocabularies. Every non-absence arm
        /// is either found through a real configured six-hero definition and backed by focused
        /// behaviour evidence, or carries an explicit unsupported disposition naming the source that
        /// would admit it. No synthetic definition is created merely to make the enum look complete.
        /// </summary>
        [Test]
        public void EveryFiniteRelationVocabularyArm_HasAReachableDeclarationOrANamedExclusion()
        {
            var declared = ReachableVerifiedRelationDeclarations();
            var vocabulary = Defined<SourceTeamRelation>(SourceTeamRelation.None)
                .Select(value => $"SourceTeamRelation/{value}")
                .Concat(Defined<RestrictedActorRelation>(RestrictedActorRelation.None)
                    .Select(value => $"RestrictedActorRelation/{value}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            var namedTwice = declared.Intersect(
                    UnsupportedRelationMembers.Keys, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(namedTwice, Is.Empty,
                "A real configured declarer cannot also be called unsupported: " + Join(namedTwice));

            var undecided = vocabulary
                .Where(value => !declared.Contains(value, StringComparer.Ordinal) &&
                    !UnsupportedRelationMembers.ContainsKey(value))
                .ToArray();
            Assert.That(undecided, Is.Empty,
                "A finite relation arm has neither a real independently reachable declarer nor an " +
                "explicit unsupported disposition with an admission trigger: " + Join(undecided));

            var missingEvidence = declared
                .Where(value => !RelationBehaviourEvidence.ContainsKey(value) ||
                    string.IsNullOrWhiteSpace(RelationBehaviourEvidence[value]))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(missingEvidence, Is.Empty,
                "A real relation declarer lacks named focused behaviour evidence: " +
                Join(missingEvidence));

            var unexplained = UnsupportedRelationMembers
                .Where(value => string.IsNullOrWhiteSpace(value.Value) ||
                    value.Value.IndexOf("Admit when", StringComparison.Ordinal) < 0)
                .Select(value => value.Key)
                .ToArray();
            Assert.That(unexplained, Is.Empty,
                "An unsupported arm must say which verified source would admit it: " +
                Join(unexplained));
        }

        /// <summary>
        /// The negative half of the rule above: a probe reading a narrow query proves the query reads
        /// the member, and this proves the engine reads the query. Without it a consumer could be a
        /// production type nothing calls, which is exactly what an inert declaration is.
        /// </summary>
        [Test]
        public void TheConsumingSeams_AreTheOnesTheEngineActuallyUses()
        {
            var core = typeof(RuleModificationSet).Assembly;
            var restrictionPredicate = ILConformanceScanner.Method(
                typeof(ActionRestrictionQuery), nameof(ActionRestrictionQuery.IsRestrictedFrom));
            Assert.That(restrictionPredicate, Is.Not.Null,
                "The shared action-restriction predicate no longer resolves. Correct the rule rather " +
                "than deleting it; a rule that names nothing protects nothing.");

            AssertReaches(
                typeof(GuardsDigital.Core.Cards.TurnResolutionAuthority), restrictionPredicate,
                "The turn's offer set must consult the restriction predicate, or a hero barred from " +
                "Movement, Attack or Skill is still offered the action.");
            AssertReaches(
                typeof(GuardsDigital.Core.Cards.GeneralRuleActionOfferQuery), restrictionPredicate,
                "The general-rule offers - Hold, Fast Travel, Clear - must consult it, or their " +
                "restrictions are declarable and inert.");
            // Milestone 8.11 moved the Defense response's offer rule out of the backend into
            // DefenseOfferQuery, which is where it now asks both this restriction and milestone 8.4's
            // composition question. The rule follows the consumer rather than the file it used to be in.
            AssertReaches(
                typeof(GuardsDigital.Core.Rules.CardResolution.DefenseOfferQuery),
                restrictionPredicate,
                "p.14's Defense response must consult it, or a hero barred from defending is still " +
                "offered its Defense cards.");

            // Milestone 8.35l moved both consumers from the boolean form to the one that keeps the
            // answer. The rule follows the member the engine actually uses, exactly as it followed
            // the Defense response's offer rule out of the backend at 8.11: what it guards is that
            // p.19's Swap and p.18's Place ask the directed question at all, not which of the query's
            // two spellings they ask it in. Asserting the boolean form alone would now pass only while
            // the reason a player is shown is thrown away.
            var directedRestriction = ILConformanceScanner.Method(
                typeof(RestrictionQuery), nameof(RestrictionQuery.TryGetRestriction));
            Assert.That(directedRestriction, Is.Not.Null,
                "The directed restriction query no longer resolves; correct the rule rather than " +
                "deleting it.");
            Assert.That(
                ILConformanceScanner.Method(
                    typeof(RestrictionQuery), nameof(RestrictionQuery.IsRestricted)),
                Is.Not.Null,
                "The boolean form is the one the undirected offer seams still read; it resolving is " +
                "what makes the choice of form above a real choice.");
            AssertReaches(
                typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticExchangeCapabilities),
                directedRestriction,
                "p.19's Swap must ask whether this actor may swap this piece, or Magnetic Dagger's " +
                "qualification has no consumer.");
            AssertReaches(
                typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticPlacementCapabilities),
                directedRestriction,
                "p.18's Place must ask the same question, or half of the same sentence has none.");

            var overlayRead = ILConformanceScanner.Method(
                typeof(RuleModificationSet), nameof(RuleModificationSet.AnyOverlayMakesAnObstacleOf));
            Assert.That(overlayRead, Is.Not.Null,
                "The overlay read no longer resolves; correct the rule rather than deleting it.");
            AssertReaches(
                typeof(ObstacleQuery), overlayRead,
                "p.18's obstacle classification must read the declared overlays, or a barrier closes " +
                "nothing.");

            // The overlay reaches movement, push and place because all three ask the one query. Each
            // link is asserted rather than assumed: a caller that stopped asking would leave this rule
            // passing while its own spaces quietly reopened.
            var obstacleMembers = new MemberInfo[]
            {
                ILConformanceScanner.Method(typeof(ObstacleQuery), nameof(ObstacleQuery.IsObstacle)),
                ILConformanceScanner.Method(
                    typeof(ObstacleQuery), nameof(ObstacleQuery.BlocksTransit)),
                ILConformanceScanner.Method(
                    typeof(ObstacleQuery), nameof(ObstacleQuery.BlocksEndOfMovement)),
                ILConformanceScanner.Method(
                    typeof(ObstacleQuery), nameof(ObstacleQuery.BlocksPlacement))
            };
            Assert.That(obstacleMembers, Has.None.Null,
                "An obstacle question no longer resolves; correct the rule rather than deleting it.");

            AssertReachesAny(
                typeof(GuardsDigital.Core.Rules.Movement.MovementReachabilityQuery), obstacleMembers,
                "Movement must ask the one obstacle query.");
            AssertReachesAny(
                typeof(GuardsDigital.Core.Rules.Displacement.PushPathQuery), obstacleMembers,
                "Push must ask the one obstacle query.");
            AssertReachesAny(
                typeof(GuardsDigital.Core.Rules.Placement.PlaceDestinationQuery), obstacleMembers,
                "Place legality must ask the one obstacle query - the acceptance line that puts " +
                "movement, push and place on one classification.");

            // Milestone 8.19c. The four value totals whose probes read a query, and the engine paths
            // that must ask those queries. Without these links a total could be composed correctly by
            // a type nothing calls, which is the inert declaration this whole rule is about.
            var attackTotal = ILConformanceScanner.Method(
                typeof(AttackValueQuery), nameof(AttackValueQuery.Calculate));
            var movementTotal = ILConformanceScanner.Method(
                typeof(MovementValueQuery), nameof(MovementValueQuery.Calculate));
            Assert.That(new MemberInfo[] { attackTotal, movementTotal }, Has.None.Null,
                "A promoted value total no longer resolves; correct the rule rather than deleting it.");
            // The two backends are internal to Core, so they are located through the assembly rather
            // than named in source: this test assembly compiles against Core's public surface even
            // though the single-assembly harness sees everything. Naming them here would build in one
            // tier and not the other, which is exactly the trap milestone 8.12's closing check hit.
            var attackBackend = CoreType(
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend");
            var movementBackend = CoreType(
                "GuardsDigital.Core.Rules.CardResolution.MovementCardActionStrategyBackend");
            AssertReaches(
                attackBackend,
                attackTotal,
                "p.14's attack must compose its total through the one Attack query, or a source's " +
                "declared Attack reaches the query and not the attack.");
            AssertReaches(
                movementBackend,
                movementTotal,
                "p.11's Movement action must spend the allowance the one Movement query composes.");

            // Range is asked through either of its two forms - the offer overload or the identity one -
            // and both compose the same total, so the link is satisfied by whichever the caller took.
            //
            // MILESTONE 8.70 MADE IT TWO LINKS RATHER THAN ONE, and the rule follows the member the
            // engine actually uses - which is what it already did at 8.11, when p.14's Defense offer
            // moved out of this backend, and at 8.35l, when two consumers moved to the explaining form
            // of the restriction query. The reach a Ranged attack composes is now asked by a SECOND
            // moment as well: a modal Attack's bullet prints "in range" and the modal choice reads it
            // one whole action before that bullet's own sequence does. Both moments go through
            // `AttackSequenceBulletSubjectQuery`, which is the one place the Range Total for an attack
            // is composed, and this backend delegates to it. Asserting the backend still named
            // `RangeValueQuery` directly would have been asserting which file the composition lives
            // in; asserting the chain is what keeps the guarantee - that a source's declared Range
            // reaches the attack rather than only the query.
            var rangeTotals = ILConformanceScanner
                .Methods(typeof(RangeValueQuery), nameof(RangeValueQuery.Calculate))
                .Cast<MemberInfo>()
                .ToArray();
            Assert.That(rangeTotals, Is.Not.Empty,
                "The Range total no longer resolves; correct the rule rather than deleting it.");
            var attackRangeComposition = ILConformanceScanner.Method(
                typeof(AttackSequenceBulletSubjectQuery),
                nameof(AttackSequenceBulletSubjectQuery.RangeValue));
            Assert.That(attackRangeComposition, Is.Not.Null,
                "The attack's Range composition no longer resolves; correct the rule rather than " +
                "deleting it.");
            AssertReaches(
                attackBackend,
                attackRangeComposition,
                "p.14's attack must compose its reach through the one place an attack's Range Total " +
                "is composed, or its own reach and the reach its modal bullets are judged by become " +
                "two numbers.");
            AssertReachesAny(
                typeof(AttackSequenceBulletSubjectQuery),
                rangeTotals,
                "A Ranged attack's reach must come from the one Range query.");

            // The Initiative probe drives the authority itself, so its second link is the turn asking
            // that authority - the path p.7's order actually runs through.
            var selectActor = ILConformanceScanner.Method(
                typeof(InitiativeResolutionAuthority),
                nameof(InitiativeResolutionAuthority.TrySelectNextActor));
            Assert.That(selectActor, Is.Not.Null,
                "Initiative selection no longer resolves; correct the rule rather than deleting it.");
            AssertReaches(
                typeof(GuardsDigital.Core.Cards.TurnResolutionAuthority), selectActor,
                "p.7's turn order must run through the Initiative authority, or a declared Initiative " +
                "modifier changes a total nobody orders by.");

            // Milestone 8.19c's overlay: one terrain question, and the roster capability that asks it.
            var terrainRead = ILConformanceScanner.Method(
                typeof(TerrainPresenceQuery), nameof(TerrainPresenceQuery.IsTerrain));
            var adjacentTerrain = ILConformanceScanner.Method(
                typeof(TerrainPresenceQuery), nameof(TerrainPresenceQuery.IsAdjacentToTerrain));
            Assert.That(new MemberInfo[] { terrainRead, adjacentTerrain }, Has.None.Null,
                "A terrain question no longer resolves; correct the rule rather than deleting it.");
            AssertReaches(
                typeof(TerrainPresenceQuery), terrainRead,
                "The adjacency form must ask the space form, or the two sources of terrain are read " +
                "in two places.");
            AssertReaches(
                typeof(SyntheticRosterCapabilities), adjacentTerrain,
                "The roster's adjacent-terrain presence must ask the one terrain question, or a hero " +
                "petrified into terrain is terrain to every rule but the one that looks for it.");
        }

        /// <summary>
        /// One Core type located by name, including an internal one. Milestone 8.19c added it so a rule
        /// can name a seam the Unity Edit Mode assembly cannot see at compile time; a name that stops
        /// resolving fails here rather than narrowing the scan in silence.
        /// </summary>
        private static Type CoreType(string fullName)
        {
            var type = typeof(RuleModificationSet).Assembly.GetType(fullName, false);
            Assert.That(type, Is.Not.Null,
                $"'{fullName}' no longer resolves in the Core assembly. Correct the rule rather than " +
                "deleting it; a rule that names nothing protects nothing.");
            return type;
        }

        private static void AssertReaches(Type subject, MemberInfo member, string because)
        {
            Assert.That(subject, Is.Not.Null,
                "A consuming seam no longer resolves by name; correct the rule rather than letting " +
                "the scan narrow silently.");
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(subject), member),
                Is.True,
                because);
        }

        private static void AssertReachesAny(
            Type subject, IReadOnlyList<MemberInfo> members, string because)
        {
            Assert.That(subject, Is.Not.Null,
                "A consuming seam no longer resolves by name; correct the rule rather than letting " +
                "the scan narrow silently.");
            var types = ILConformanceScanner.WithNestedTypes(subject);
            Assert.That(
                members.Any(member => ILConformanceScanner.References(types, member)),
                Is.True,
                because);
        }

        private static string Join(IEnumerable<string> values) => string.Join(", ", values);

        /// <summary>
        /// Every modification a definition can declare, spelled family-then-member and enumerated from
        /// the enums rather than from a list, so a new member is undecided until someone decides it.
        /// </summary>
        private static IReadOnlyList<string> DeclarableMembers()
        {
            var activities = Defined<RuleActivity>(RuleActivity.None);
            return Defined<PrintedValueChannel>(PrintedValueChannel.None)
                .Select(value => $"ValueModifier/{value}")
                .Concat(activities.Select(value => $"Restriction/{value}"))
                .Concat(Defined<RulePermissionKind>(RulePermissionKind.None)
                    .Select(value => $"Permission/{value}"))
                .Concat(activities.Select(value => $"Immunity/{value}"))
                .Concat(Defined<EffectiveUnitKind>(EffectiveUnitKind.None)
                    .Select(value => $"CountsAs/{value}"))
                // Milestone 8.19c's sixth family. Enumerated from the same enum as p.17's replacement,
                // because a type that can be replaced by is a type that can be added, and a member
                // added to either family must be undecided in both until someone decides it.
                .Concat(Defined<EffectiveUnitKind>(EffectiveUnitKind.None)
                    .Select(value => $"EffectiveTypeOverlay/{value}"))
                .Concat(Defined<ObstacleOverlayArm>(ObstacleOverlayArm.None)
                    .Select(value => $"ObstacleOverlay/{value}"))
                .ToArray();
        }

        private static IReadOnlyList<TEnum> Defined<TEnum>(TEnum absent) where TEnum : struct =>
            Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Where(value => !value.Equals(absent))
                .ToArray();

        private static IReadOnlyList<string> ReachableVerifiedRelationDeclarations()
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(
                TeamSide.Red);
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var verifiedRevisions = new HashSet<string>(
                configuration.CardDefinitions.Select(value => value.ContentRevision),
                StringComparer.Ordinal);
            var effects = configuration.CardDefinitions
                .SelectMany(card => card.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(definition => definition != null)
                .Concat(catalogs.TriggeredEffects.Definitions
                    .Where(definition => verifiedRevisions.Contains(definition.ContentRevision)))
                .GroupBy(definition => definition.Reference)
                .Select(group => group.First())
                .ToArray();

            var sourceRelations = new HashSet<SourceTeamRelation>();
            var activeDefinitions = new Dictionary<string, ActiveEffectDefinition>(
                StringComparer.Ordinal);
            var relationParameterNames = new HashSet<string>(StringComparer.Ordinal)
            {
                SyntheticDisplacementCapabilities.TargetRelationParameterName,
                SyntheticRosterCapabilities.RelationParameter,
                SyntheticTargetingCapabilities.TargetRelationParameterName
            };
            foreach (var effect in effects)
            {
                foreach (var node in effect.Nodes)
                {
                    foreach (var input in node.Inputs.Where(value =>
                        value.SourceKind == EffectBindingSourceKind.Literal &&
                        value.ValueKind == EffectValueKind.Integer &&
                        relationParameterNames.Contains(value.ParameterName)))
                    {
                        var relation = (SourceTeamRelation)input.Literal.IntegerValue;
                        if (relation != SourceTeamRelation.None &&
                            Enum.IsDefined(typeof(SourceTeamRelation), relation))
                        {
                            sourceRelations.Add(relation);
                        }
                    }

                    if (node.Capability.IsDefined &&
                        catalogs.Capabilities.TryLoad(node.Capability, out var handler))
                    {
                        foreach (var definition in handler.ActiveEffectDefinitions)
                        {
                            activeDefinitions[definition.DefinitionId] = definition;
                        }
                    }
                }
            }

            var restrictedRelations = new HashSet<RestrictedActorRelation>();
            foreach (var definition in activeDefinitions.Values)
            {
                if (definition.AffectedUnitRelation != SourceTeamRelation.None)
                {
                    sourceRelations.Add(definition.AffectedUnitRelation);
                }

                foreach (var relation in definition.Modifications
                    .Where(modification => modification.Kind == RuleModificationKind.Restriction)
                    .SelectMany(modification => modification.RestrictedActors.Relations))
                {
                    restrictedRelations.Add(relation);
                }
            }

            return sourceRelations.Select(value => $"SourceTeamRelation/{value}")
                .Concat(restrictedRelations.Select(value =>
                    $"RestrictedActorRelation/{value}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static readonly IReadOnlyDictionary<string, string> RelationBehaviourEvidence =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["SourceTeamRelation/OpposingSource"] =
                    "ConfiguredAttackCardTests and the per-hero configured effect suites",
                ["SourceTeamRelation/AlliedWithSource"] =
                    "BroganAlliedHeroCardTests plus SabinaMinionCommandTests",
                ["SourceTeamRelation/AnyTeam"] =
                    "RosterTargetGeometryCapabilityTests and TargetQueryTests",
                ["SourceTeamRelation/OpposingSourceOrUnaligned"] =
                    "Brogan's configured push-card cases",
                ["RestrictedActorRelation/AffectedObjectItself"] =
                    "ConfiguredModificationSkillTests.MagneticDagger",
                ["RestrictedActorRelation/HeroesOpposingSource"] =
                    "ConfiguredModificationSkillTests and BroganAlliedHeroCardTests",
                ["RestrictedActorRelation/AnyActor"] =
                    "Arien's configured Spell Break restriction cases"
            };

        // Filled only by a source-backed unsupported decision. HeroesAlliedWithSource has no
        // verified six-hero declarer: the existing relation-bearing restrictions are enemy-only.
        private static readonly IReadOnlyDictionary<string, string> UnsupportedRelationMembers =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RestrictedActorRelation/HeroesAlliedWithSource"] =
                    "No verified six-hero source restricts heroes allied with its source. " +
                    "Admit when a verified card's restriction names friendly or allied heroes as " +
                    "actors barred from repositioning or another declared activity."
            };

        /// <summary>
        /// The members no production rule reads, each with why not and where its consumer would go.
        ///
        /// Every entry is a decision rather than an omission, and the two kinds of reason are kept
        /// apart deliberately. A value channel is excluded because no verified or synthetic source
        /// declares one and milestone 8.7's own non-goal forbids inventing a consumer for a rule no
        /// card prints - so each names the single query that owns that channel's total, which is where
        /// one line goes when a card first raises it. The rest are excluded because the rules do not
        /// pose the question at all, and those entries say what the rulebook says instead.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> ExcludedMembers =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Milestone 8.19c consumed four of the five value channels this list used to carry.
                // The roster's own sources are what raised them: Sabina's Big Sodding Gun declares
                // Attack and Range, Xargatha's Metamorphosis declares Movement and Initiative, and
                // Tigerclaw's poison marker declares Initiative, Attack and Defense. Each moved to a
                // consumption case naming the query that now composes it.
                ["ValueModifier/Radius"] =
                    "No source declares one. p.9's Radius Total is composed in RadiusValueQuery.",
                ["Restriction/ObjectPush"] =
                    "p.18 protects a unit from being pushed through immunity, which is consumed; " +
                    "nothing in the rulebook or in any verified source makes a unit unable to " +
                    "perform a push, so a restriction naming it would forbid nothing that happens.",
                ["Restriction/DirectMove"] =
                    "The verified sources protect a unit from card-text direct Move through " +
                    "immunity, which is consumed; none restricts a unit from performing that " +
                    "operation, so a restriction naming it would forbid nothing that happens.",
                ["Immunity/Movement"] =
                    "p.18's immunity is a targeting rule - \"you cannot target that unit\" - and a " +
                    "Movement action targets no unit. ActionDefinition also refuses a Movement " +
                    "effect root, so no card text can ask from inside one either.",
                ["Immunity/Hold"] =
                    "p.12's Hold does nothing to anyone, and its definition suppresses its source " +
                    "card's text, so nothing inside a Hold can ask an immunity question.",
                ["Immunity/FastTravel"] =
                    "p.12's Fast Travel moves the acting hero alone and suppresses its source " +
                    "card's text, so it targets no unit to be immune.",
                ["Immunity/Clear"] =
                    "p.13's Clear removes the acting hero's adjacent tokens, and p.19 states tokens " +
                    "are not units; the replacement also suppresses its source card's text.",
                ["Immunity/Repeat"] =
                    "Milestone 8.35z28 added the activity for Dodger's Enfeeblement, which " +
                    "restricts it. p.18's immunity is a targeting rule - \"you cannot target that " +
                    "unit\" - and p.10's Repeat targets nobody: it re-performs an action whose own " +
                    "targeting asks the immunity question again on its own account. An immunity " +
                    "naming it would protect a unit from something that never points at one.",
                ["CountsAs/Hero"] =
                    "p.17's replacement is declared toward minion by every verified and synthetic " +
                    "source. No rule asks whether an object counts as a hero: ownership, occupancy, " +
                    "the turn's actor and p.18's miniature exception all read intrinsic identity, " +
                    "which IntrinsicKindQuery answers and a Counts As declaration cannot move.",
                ["CountsAs/Terrain"] =
                    "Not declarable at all: RuleModification.ForCountsAs refuses it, because p.17 " +
                    "replaces \"all rules and qualities\" and p.18's terrain has none a unit could be " +
                    "run by. Milestone 8.19c's effective-type overlay is the only way a card adds it.",
                ["EffectiveTypeOverlay/Hero"] =
                    "No source adds one. The roster's one overlay is Xargatha's Stone Gaze, which " +
                    "adds terrain to heroes that already are heroes; an overlay adding Hero would " +
                    "answer a question no rule asks, for the reason CountsAs/Hero records.",
                ["EffectiveTypeOverlay/Minion"] =
                    "No source adds one. A card that makes something count as a minion says so as " +
                    "p.17's replacement, which is CountsAs/Minion and is consumed; adding the type " +
                    "beside an intrinsic hero would put one unit on both sides of the Minion Battle."
            };

        /// <summary>One member, the production seam that reads it, and the answer that moves.</summary>
        private sealed class ConsumptionCase
        {
            public ConsumptionCase(
                string member,
                string seam,
                RuleModificationSet declared,
                Func<RuleModificationSet, string> read)
            {
                Member = member;
                Seam = seam;
                Declared = declared;
                Read = read;
            }

            public string Member { get; }

            /// <summary>The production seam the probe reads, named for the failure message.</summary>
            public string Seam { get; }

            /// <summary>A live set declaring exactly this member.</summary>
            public RuleModificationSet Declared { get; }

            /// <summary>The seam's answer, rendered so two runs can be compared as text.</summary>
            public Func<RuleModificationSet, string> Read { get; }
        }

        private static IReadOnlyList<ConsumptionCase> ConsumptionCases(SeamFixture fixture)
        {
            var blue = RuleObject.ForHero(fixture.Blue);
            var cases = new List<ConsumptionCase>
            {
                new ConsumptionCase(
                    "ValueModifier/Defense",
                    "DefenseValueQuery.Calculate",
                    Declaring(RuleModification.ForValue(PrintedValueChannel.Defense, 3), blue, fixture),
                    fixture.ReadDefenseTotal),
                new ConsumptionCase(
                    "Permission/MoveThroughObstacles",
                    "MovementReachabilityQuery.FindReachableDestinations",
                    Declaring(
                        RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadReachableDestinations),
                // Milestone 8.114b1. Wuk's Trample: obstacles are crossed only along a straight walk,
                // read on the Movement action's offered destinations.
                new ConsumptionCase(
                    "Permission/MoveThroughObstaclesInAStraightLine",
                    "StraightLineCrossingQuery.Widen",
                    Declaring(
                        RuleModification.ForPermission(
                            RulePermissionKind.MoveThroughObstaclesInAStraightLine, RuleActivity.Movement),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadStraightLineDestinations),
                // Milestone 8.112c. Mrak's Stone Carapace and Rock Solid grant their OWN resolved card
                // as a substitute: the discard handlers read the first grant through the substitution
                // query, and p.14's Defense window reads the second through its offer query.
                new ConsumptionCase(
                    "Permission/DiscardSourceCardInsteadOfHandCard",
                    "SourceCardSubstitutionQuery.Substitutes (discard-chosen-card, discard-owned-card)",
                    DeclaringFromCard(
                        RuleModification.ForPermission(RulePermissionKind.DiscardSourceCardInsteadOfHandCard),
                        blue, fixture),
                    fixture.ReadDiscardSubstitutes),
                new ConsumptionCase(
                    "Permission/DefendWithSourceCardAsIfInHand",
                    "DefenseOfferQuery.Offers",
                    DeclaringFromCard(
                        RuleModification.ForPermission(RulePermissionKind.DefendWithSourceCardAsIfInHand),
                        blue, fixture),
                    fixture.ReadDefenseOffers),
                new ConsumptionCase(
                    "CountsAs/Minion",
                    "BattleZoneQuery.FindEffectiveMinionsInside",
                    Declaring(RuleModification.ForCountsAs(EffectiveUnitKind.Minion), blue, fixture),
                    fixture.ReadEffectiveBattleZoneMinions),

                // Milestone 8.19c. The four value channels the roster's own sources raised, each at
                // the query that composes that channel's total. Every one is declared on the hero the
                // probe's total belongs to, so a channel wired to the wrong object would read as an
                // unmoved answer rather than passing by coincidence.
                new ConsumptionCase(
                    "ValueModifier/Attack",
                    "AttackValueQuery.Calculate",
                    Declaring(
                        RuleModification.ForValue(PrintedValueChannel.Attack, 2),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadAttackTotal),
                new ConsumptionCase(
                    "ValueModifier/Range",
                    "RangeValueQuery.Calculate",
                    Declaring(
                        RuleModification.ForValue(PrintedValueChannel.Range, 2),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadRangeTotal),
                new ConsumptionCase(
                    "ValueModifier/Movement",
                    "MovementValueQuery.Calculate",
                    Declaring(
                        RuleModification.ForValue(PrintedValueChannel.Movement, 2),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadMovementTotal),
                new ConsumptionCase(
                    "ValueModifier/Initiative",
                    "InitiativeResolutionAuthority.TrySelectNextActor",
                    Declaring(
                        RuleModification.ForValue(PrintedValueChannel.Initiative, 3),
                        RuleObject.ForHero(fixture.Red),
                        fixture),
                    fixture.ReadSelectedInitiativeTotal),

                // Milestone 8.19c's overlay. The probe asks about the space the Blue hero is standing
                // on rather than about the acting hero's neighbourhood, because the fixture's two
                // spaces may already sit beside printed terrain and an answer that was true before the
                // declaration would prove nothing. A playable space an ordinary hero occupies is not
                // terrain; the overlay is the whole of what makes it one.
                new ConsumptionCase(
                    "EffectiveTypeOverlay/Terrain",
                    "TerrainPresenceQuery.IsTerrain",
                    Declaring(
                        RuleModification.ForEffectiveTypeOverlay(EffectiveUnitKind.Terrain),
                        blue,
                        fixture),
                    fixture.ReadTerrainUnderBlue)
            };

            // Every action kind reaches its activity through one mapping, so one predicate covers the
            // family; the seam links above prove the offer queries ask it.
            foreach (var actionKind in new[]
            {
                ActionKind.Movement, ActionKind.Attack, ActionKind.Skill, ActionKind.Defense,
                ActionKind.Hold, ActionKind.FastTravel, ActionKind.Clear
            })
            {
                var activity = RuleActivityQuery.ForAction(actionKind);
                cases.Add(new ConsumptionCase(
                    $"Restriction/{activity}",
                    $"ActionRestrictionQuery.IsRestrictedFrom({actionKind})",
                    Declaring(
                        RuleModification.ForRestriction(activity), RuleObject.ForHero(fixture.Red),
                        fixture),
                    set => fixture.ReadActionRestriction(actionKind, set)));
            }

            // Milestone 8.35z28. p.10's Repeat is the one activity no ActionKind maps to, so the
            // loop above cannot reach it: it is a control-flow modifier on an action rather than an
            // action anybody performs. Dodger's Enfeeblement declares it - "cannot repeat actions" -
            // and RepeatRestrictionQuery is the production query both of CardActionResolutionAuthority's
            // repeat gates ask, which is the second link TheConsumingSeams_AreTheOnesTheEngineActuallyUses
            // supplies.
            cases.Add(new ConsumptionCase(
                "Restriction/Repeat",
                "RepeatRestrictionQuery.IsRestrictedFromRepeating",
                Declaring(
                    RuleModification.ForRestriction(RuleActivity.Repeat),
                    RuleObject.ForHero(fixture.Red),
                    fixture),
                set => RepeatRestrictionQuery
                    .IsRestrictedFromRepeating(fixture.Red, set)
                    .ToString()));

            // p.18's unqualified "if a unit is immune" reaches every activity, so it is proven where
            // any specific one is: declaring All must move the same answer Attack moves.
            cases.Add(new ConsumptionCase(
                "Restriction/All",
                "ActionRestrictionQuery.IsRestrictedFrom(Movement)",
                Declaring(
                    RuleModification.ForRestriction(RuleActivity.All), RuleObject.ForHero(fixture.Red),
                    fixture),
                set => fixture.ReadActionRestriction(ActionKind.Movement, set)));
            cases.Add(new ConsumptionCase(
                "Immunity/All",
                "AttackTargetQuery.FindLegalEnemyTargets",
                Declaring(RuleModification.ForImmunity(RuleActivity.All), blue, fixture),
                fixture.ReadLegalAttackTargets));
            cases.Add(new ConsumptionCase(
                "Immunity/Attack",
                "AttackTargetQuery.FindLegalEnemyTargets",
                Declaring(RuleModification.ForImmunity(RuleActivity.Attack), blue, fixture),
                fixture.ReadLegalAttackTargets));
            cases.Add(new ConsumptionCase(
                "Immunity/ObjectPush",
                "SyntheticDisplacementCapabilities push target derivation",
                Declaring(RuleModification.ForImmunity(RuleActivity.ObjectPush), blue, fixture),
                fixture.ReadPushAnticipation));
            cases.Add(new ConsumptionCase(
                "Immunity/DirectMove",
                "SyntheticDisplacementCapabilities direct Move target derivation",
                Declaring(RuleModification.ForImmunity(RuleActivity.DirectMove), blue, fixture),
                fixture.ReadDirectMoveAnticipation));

            // The enclosing action's own activity is what a capability asks immunity with, so the two
            // action kinds that may carry an effect root beside a Skill are proven the same way: the
            // same Swap node, judged inside each.
            cases.Add(new ConsumptionCase(
                "Immunity/Skill",
                "SyntheticExchangeCapabilities Swap inside a Skill",
                Declaring(RuleModification.ForImmunity(RuleActivity.Skill), blue, fixture),
                set => fixture.ReadSwapAnticipation(ActionKind.Skill, set)));
            cases.Add(new ConsumptionCase(
                "Immunity/Defense",
                "SyntheticExchangeCapabilities Swap inside a Defense",
                Declaring(RuleModification.ForImmunity(RuleActivity.Defense), blue, fixture),
                set => fixture.ReadSwapAnticipation(ActionKind.Defense, set)));
            cases.Add(new ConsumptionCase(
                "Immunity/Swap",
                "SyntheticExchangeCapabilities Swap target derivation",
                Declaring(RuleModification.ForImmunity(RuleActivity.Swap), blue, fixture),
                set => fixture.ReadSwapAnticipation(ActionKind.Skill, set)));
            cases.Add(new ConsumptionCase(
                "Immunity/Place",
                "SyntheticPlacementCapabilities Place piece derivation",
                Declaring(RuleModification.ForImmunity(RuleActivity.Place), blue, fixture),
                fixture.ReadPlaceAnticipation));

            // Magnetic Dagger's own shape: the piece is restricted, and the actor is an enemy hero of
            // the source. Wasp's teammates are unrestricted, which is what makes the qualification
            // observable rather than decorative.
            cases.Add(new ConsumptionCase(
                "Restriction/Swap",
                "SyntheticExchangeCapabilities Swap target derivation",
                Declaring(QualifiedRepositioningRestriction(RuleActivity.Swap), blue, fixture),
                set => fixture.ReadSwapAnticipation(ActionKind.Skill, set)));
            cases.Add(new ConsumptionCase(
                "Restriction/Place",
                "SyntheticPlacementCapabilities Place piece derivation",
                Declaring(QualifiedRepositioningRestriction(RuleActivity.Place), blue, fixture),
                fixture.ReadPlaceAnticipation));

            // One distant empty space answers both arms. Anchored on itself the acting hero is outside
            // the disk and the space is inside it; anchored on the acting hero the hero is inside and
            // the space is beyond. Either way an ordinary empty space becomes an obstacle, which is
            // the whole of what an overlay does.
            cases.Add(new ConsumptionCase(
                "ObstacleOverlay/SpacesInsideWhileActorOutside",
                "ObstacleQuery.IsObstacle",
                fixture.OverlaySet(ObstacleOverlayArm.SpacesInsideWhileActorOutside),
                set => fixture.ReadObstacle(fixture.OverlayProbeSpace, set)));
            cases.Add(new ConsumptionCase(
                "ObstacleOverlay/SpacesOutsideWhileActorInside",
                "ObstacleQuery.IsObstacle",
                fixture.OverlaySet(ObstacleOverlayArm.SpacesOutsideWhileActorInside),
                set => fixture.ReadObstacle(fixture.OverlayProbeSpace, set)));

            return cases;
        }

        /// <summary>
        /// Magnetic Dagger's restriction without its card: the affected piece may not be repositioned
        /// by itself or by a hero opposing the source, and the source's own side is unaffected.
        /// </summary>
        private static RuleModification QualifiedRepositioningRestriction(RuleActivity activity) =>
            RuleModification.ForRestriction(
                activity,
                RestrictedActorScope.Of(
                    RestrictedActorRelation.AffectedObjectItself,
                    RestrictedActorRelation.HeroesOpposingSource));

        /// <summary>
        /// A live set declaring exactly one modification, as an Active effect the Blue hero controls -
        /// so a qualification about "heroes opposing the source" names the Red hero, who is the actor
        /// in every probe below.
        /// </summary>
        /// <summary>
        /// Milestone 8.112c. A grant whose source is a resolved card of the affected hero's - the
        /// shape a substitution permission is read in.
        /// </summary>
        private static RuleModificationSet DeclaringFromCard(
            RuleModification modification, RuleObject affected, SeamFixture fixture) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        modification,
                        affected,
                        new ActiveEffectId("modification-seam-probe"),
                        fixture.Blue,
                        TeamSide.Blue,
                        0,
                        sourceCardInstanceId: fixture.ResolvedSubstitute.Id)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        private static RuleModificationSet Declaring(
            RuleModification modification, RuleObject affected, SeamFixture fixture) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        modification,
                        affected,
                        new ActiveEffectId("modification-seam-probe"),
                        fixture.Blue,
                        TeamSide.Blue,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// One board the probes above all read, built the way the official match builds one and then
        /// arranged so that every question has a subject: the two heroes stand adjacent inside the
        /// current Battle Zone, and one empty space is kept well away from both for the overlay.
        ///
        /// The Red hero acts in every probe and the Blue hero is the subject, so a qualification about
        /// "heroes opposing the source" - the source being Blue's - names Red, which is what makes
        /// Magnetic Dagger's shape observable here.
        /// </summary>
        private sealed class SeamFixture
        {
            private const int OverlayRadius = 1;

            private SeamFixture(
                MatchConfiguration configuration,
                AdjacentMoveAuthority board,
                HeroId red,
                HeroId blue,
                HexCoordinate redAt,
                HexCoordinate blueAt,
                HexCoordinate overlayProbeSpace)
            {
                Configuration = configuration;
                Board = board;
                Red = red;
                Blue = blue;
                RedAt = redAt;
                BlueAt = blueAt;
                OverlayProbeSpace = overlayProbeSpace;
            }

            public MatchConfiguration Configuration { get; }

            public AdjacentMoveAuthority Board { get; }

            public MatchParticipants Participants => Configuration.Participants;

            public HeroId Red { get; }

            public HeroId Blue { get; }

            public HexCoordinate RedAt { get; }

            public HexCoordinate BlueAt { get; }

            /// <summary>An empty playable space far enough from the acting hero to be outside a disk of
            /// <see cref="OverlayRadius"/> drawn around it.</summary>
            public HexCoordinate OverlayProbeSpace { get; }

            public static SeamFixture Create()
            {
                var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);
                var board = new AdjacentMoveAuthority(
                    map,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                    Array.Empty<ObstacleOccupancy>(),
                    setup.Minions);

                var red = configuration.Participants.Heroes
                    .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                    .Id;
                var blue = configuration.Participants.Heroes
                    .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                    .Id;

                var battleZone = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
                var pair = battleZone
                    .Where(board.IsEmptyPlayableSpace)
                    .SelectMany(first => first.GetNeighbors()
                        .Where(second => battleZone.Contains(second) && board.IsEmptyPlayableSpace(second))
                        .Select(second => new { First = first, Second = second }))
                    .FirstOrDefault();
                Assert.That(pair, Is.Not.Null,
                    "The fixture needs two adjacent empty Battle-Zone spaces to stand the heroes on; " +
                    "the official setup no longer leaves any, so the probes below would answer about " +
                    "nothing.");

                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(red), pair.First).Accepted, Is.True);
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(blue), pair.Second).Accepted, Is.True);

                var overlaySpace = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .OrderByDescending(candidate => HexDistanceQuery.Calculate(candidate, pair.First))
                    .First();
                Assert.That(
                    HexDistanceQuery.Calculate(overlaySpace, pair.First),
                    Is.GreaterThan(OverlayRadius),
                    "The overlay probe space must lie outside a disk drawn around the acting hero, or " +
                    "the two arms would ask the same question.");

                return new SeamFixture(
                    configuration, board, red, blue, pair.First, pair.Second, overlaySpace);
            }

            public string ReadDefenseTotal(RuleModificationSet modifications) =>
                DefenseValueQuery.Calculate(
                        DefenseOffer(), Configuration.CardDefinitions, Blue, Participants, Board,
                        modifications, null)
                    .Total.ToString(CultureInfo.InvariantCulture);

            /// <summary>
            /// Milestone 8.112c. One resolved card of the Blue hero's with a printed secondary Defense,
            /// the subject both substitution grants name.
            /// </summary>
            public CardInstance ResolvedSubstitute => resolvedSubstitute ?? (resolvedSubstitute = CreateResolvedSubstitute());

            private CardInstance resolvedSubstitute;

            private static readonly CardDefinition SubstituteDefinition = new CardDefinition(
                new CardDefinitionId("modification-seam-substitute"), "rev-1", "TEST - Substitute",
                new InitiativeValue(3), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                    new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 6) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });

            private CardInstance CreateResolvedSubstitute()
            {
                var card = CardInstance.CreateInHand(
                    new CardInstanceId("modification-seam-substitute-card"),
                    SubstituteDefinition.Reference, Blue);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
                return card;
            }

            public string ReadDiscardSubstitutes(RuleModificationSet modifications) =>
                string.Join(",", SourceCardSubstitutionQuery.Substitutes(
                        modifications, Blue, RulePermissionKind.DiscardSourceCardInsteadOfHandCard,
                        new[] { ResolvedSubstitute })
                    .Select(card => card.Id.Value));

            public string ReadDefenseOffers(RuleModificationSet modifications) =>
                string.Join(",", DefenseOfferQuery.Offers(
                        new[] { SubstituteDefinition }, new[] { ResolvedSubstitute }, Blue, modifications)
                    .Select(offer => offer.SourceCardInstanceId.Value + "/" + offer.ActionDefinitionId.Value));

            public string ReadReachableDestinations(RuleModificationSet modifications) =>
                MovementReachabilityQuery.FindReachableDestinations(
                        RedAt, RuleObject.ForHero(Red), Board, 3, modifications)
                    .Count.ToString(CultureInfo.InvariantCulture);

            public string ReadStraightLineDestinations(RuleModificationSet modifications) =>
                StraightLineCrossingQuery.Widen(
                        MovementReachabilityQuery.FindReachableDestinations(
                            RedAt, RuleObject.ForHero(Red), Board, 3, modifications),
                        new[] { RedAt }, Red, Board, 3, modifications)
                    .Count.ToString(CultureInfo.InvariantCulture);

            public string ReadEffectiveBattleZoneMinions(RuleModificationSet modifications) =>
                BattleZoneQuery.FindEffectiveMinionsInside(Board, Participants, modifications)
                    .Count.ToString(CultureInfo.InvariantCulture);

            public string ReadActionRestriction(ActionKind actionKind, RuleModificationSet modifications) =>
                ActionRestrictionQuery.IsRestrictedFrom(Red, actionKind, modifications, CardColor.None)
                    .ToString(CultureInfo.InvariantCulture);

            public string ReadLegalAttackTargets(RuleModificationSet modifications) =>
                AttackTargetQuery.FindLegalEnemyTargets(
                        Red, AttackRangeKind.NonRanged, 1, Participants, Board, modifications)
                    .Count.ToString(CultureInfo.InvariantCulture);

            /// <summary>
            /// Milestone 8.19c. p.13's Attack Total for an Attack the Red hero could perform, read at
            /// the query that composes the printed icon, p.16's items and the source declarations.
            /// </summary>
            public string ReadAttackTotal(RuleModificationSet modifications) =>
                AttackValueQuery.Calculate(
                        OfferOf(ActionKind.Attack), Configuration.CardDefinitions, null, modifications)
                    .Total.ToString(CultureInfo.InvariantCulture);

            public string ReadRangeTotal(RuleModificationSet modifications) =>
                RangeValueQuery.Calculate(
                        OfferOf(ActionKind.Attack), Configuration.CardDefinitions, null, modifications)
                    .Total.ToString(CultureInfo.InvariantCulture);

            public string ReadMovementTotal(RuleModificationSet modifications) =>
                MovementValueQuery.Calculate(
                        OfferOf(ActionKind.Movement), Configuration.CardDefinitions, null,
                        modifications)
                    .Total.ToString(CultureInfo.InvariantCulture);

            /// <summary>
            /// Milestone 8.19c. p.7's Initiative total, read where the turn order reads it: one
            /// revealed card belonging to the Red hero, selected as the next actor. The card is built
            /// here rather than taken from the configuration because the fixture's hand is unplayed,
            /// and the total this reports is the one the selection ordered by.
            /// </summary>
            public string ReadSelectedInitiativeTotal(RuleModificationSet modifications)
            {
                var definition = Configuration.CardDefinitions
                    .First(candidate => candidate.Initiative.Value > 0);
                var card = CardInstance.CreateInHand(
                    new CardInstanceId("modification-seam-probe-initiative"), definition.Reference, Red);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                var authority = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));
                Assert.That(
                    authority.TrySelectNextActor(
                        Configuration.CardDefinitions, new[] { card }, Participants, null,
                        modifications).Accepted,
                    Is.True,
                    "The Initiative probe must select an actor, or it reports about nothing.");
                return authority.SelectedActor.InitiativeTotal.ToString(CultureInfo.InvariantCulture);
            }

            /// <summary>
            /// Milestone 8.19c. Whether the space the Blue hero occupies counts as terrain. It is an
            /// ordinary playable space, so only an effective-type overlay on the hero standing there
            /// can make the answer true.
            /// </summary>
            public string ReadTerrainUnderBlue(RuleModificationSet modifications) =>
                TerrainPresenceQuery.IsTerrain(Board, BlueAt, modifications)
                    .ToString(CultureInfo.InvariantCulture);

            public string ReadObstacle(HexCoordinate space, RuleModificationSet modifications) =>
                ObstacleQuery.IsObstacle(Board, space, modifications)
                    .ToString(CultureInfo.InvariantCulture);

            public string ReadSwapAnticipation(
                ActionKind enclosing, RuleModificationSet modifications) =>
                Handler(SyntheticExchangeCapabilities.SwapV2)
                    .Anticipate(Context(
                        enclosing,
                        modifications,
                        new Dictionary<string, EffectValue>
                        {
                            ["first"] = EffectValue.Space(RedAt),
                            ["second"] = EffectValue.Space(BlueAt),
                            [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                                EffectValue.Integer((int)SwapEligiblePieceKind.GamePiece)
                        }))
                    .ToString();

            public string ReadPlaceAnticipation(RuleModificationSet modifications) =>
                Handler(SyntheticPlacementCapabilities.PlaceV2)
                    .Anticipate(Context(
                        ActionKind.Skill,
                        modifications,
                        new Dictionary<string, EffectValue>
                        {
                            ["subject"] = EffectValue.Space(BlueAt),
                            ["radius"] = EffectValue.Integer(2),
                            [SyntheticPlacementCapabilities.EligiblePieceParameterName] =
                                EffectValue.Integer((int)PlaceEligiblePieceKind.UnitOrToken),
                            [SyntheticPlacementCapabilities.DestinationAnchorParameterName] =
                                EffectValue.Integer((int)PlaceDestinationAnchor.PlacedPiece)
                        }))
                    .ToString();

            public string ReadPushAnticipation(RuleModificationSet modifications) =>
                Handler(SyntheticDisplacementCapabilities.PushTargetV3)
                    .Anticipate(Context(
                        ActionKind.Skill,
                        modifications,
                        new Dictionary<string, EffectValue>
                        {
                            ["target"] = EffectValue.Space(BlueAt),
                            ["distance"] = EffectValue.Integer(1),
                            ["ignoresObstacles"] = EffectValue.Boolean(false),
                            [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                                EffectValue.Integer((int)PushEligiblePieceKind.UnitOrToken)
                        }))
                    .ToString();

            public string ReadDirectMoveAnticipation(RuleModificationSet modifications) =>
                Handler(SyntheticDisplacementCapabilities.DirectMoveV3)
                    .Anticipate(Context(
                        ActionKind.Skill,
                        modifications,
                        new Dictionary<string, EffectValue>
                        {
                            ["mover"] = EffectValue.Space(BlueAt),
                            ["distance"] = EffectValue.Integer(1),
                            [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                                EffectValue.Integer((int)DirectMoveDestinationPolicy.Unrestricted)
                        }))
                    .ToString();

            /// <summary>
            /// A live set carrying one bound overlay the Blue hero's source declares, conditioned on
            /// heroes opposing it - which is the Red hero standing at <see cref="RedAt"/>.
            /// </summary>
            public RuleModificationSet OverlaySet(ObstacleOverlayArm arm)
            {
                var anchor = arm == ObstacleOverlayArm.SpacesOutsideWhileActorInside
                    ? RedAt
                    : OverlayProbeSpace;
                return new RuleModificationSet(
                    Array.Empty<AppliedRuleModification>(),
                    new[]
                    {
                        AppliedObstacleOverlay.Bind(
                            ConditionalObstacleOverlay.Declare(
                                SourceTeamRelation.OpposingSource, arm),
                            anchor,
                            OverlayRadius,
                            "modification-seam-probe-overlay",
                            Blue,
                            TeamSide.Blue,
                            TeamSide.Red,
                            RedAt)
                    },
                    TieBreakerState.Unrevealed);
            }

            /// <summary>
            /// Milestone 8.19c. One offer of the named kind the Red hero could perform, for the three
            /// value totals composed from an offer. It asks the same offer query the turn asks, so a
            /// card that stopped printing the action fails here rather than reporting about nothing.
            /// </summary>
            private ActionOffer OfferOf(ActionKind actionKind)
            {
                var offers = ActionOfferQuery.GetAvailableOffers(
                        Configuration.CardDefinitions,
                        Configuration.CardInstances,
                        Red,
                        ActionOfferWindow.NormalPlay)
                    .Where(candidate => KindOf(candidate) == actionKind)
                    .ToArray();
                Assert.That(offers, Is.Not.Empty,
                    $"The fixture hand no longer prints a {actionKind} action, so its value probe " +
                    "would answer about nothing.");
                return offers[0];
            }

            /// <summary>A Defense action the Blue hero could answer an attack with.</summary>
            private ActionOffer DefenseOffer()
            {
                // The definition is looked up from the configuration rather than through the offer
                // query's own finder, which is internal to Core and therefore invisible to the Unity
                // Edit Mode assembly even though the single-assembly harness compiles it.
                var offers = ActionOfferQuery.GetAvailableOffers(
                        Configuration.CardDefinitions,
                        Configuration.CardInstances,
                        Blue,
                        ActionOfferWindow.Reaction)
                    .Where(candidate => KindOf(candidate) == ActionKind.Defense)
                    .ToArray();
                Assert.That(offers, Is.Not.Empty,
                    "The fixture hand no longer prints a Defense action, so the Defense total probe " +
                    "would answer about nothing.");
                return offers[0];
            }

            private ActionKind KindOf(ActionOffer offer) =>
                Configuration.CardDefinitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind;

            private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
                SyntheticEffectCapabilities.CreateCatalog().Load(reference);

            private EffectCapabilityContext Context(
                ActionKind enclosing,
                RuleModificationSet modifications,
                IReadOnlyDictionary<string, EffectValue> inputs) =>
                new EffectCapabilityContext(
                    new EffectNodeId("modification-seam-probe-node"),
                    EffectFailurePolicy.Mandatory,
                    Red,
                    Red,
                    new CardInstanceId("modification-seam-probe-card"),
                    new CardDefinitionReference(
                        new CardDefinitionId("modification-seam-probe-definition"), "probe-rev-1"),
                    new ActionDefinitionId("modification-seam-probe-action"),
                    inputs,
                    1,
                    1,
                    Participants,
                    null,
                    modifications,
                    EffectCardState.Absent,
                    enclosing,
                    new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(), Board,
                        new MarkerAttachmentRegister(), Board, Board, Board));
        }
    }
}

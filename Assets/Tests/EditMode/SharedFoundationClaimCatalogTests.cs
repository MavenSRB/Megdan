using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e4b's test-owned record of deliberately claimed shared foundations. This is
    /// evidence governance, not runtime registration or rules authority.
    /// </summary>
    public sealed class SharedFoundationClaimCatalogTests
    {
        [Test]
        public void SharedFoundationClaims_RequireIndependentShapeEvidenceOrExplicitNarrowness()
        {
            Assert.That(SharedFoundationClaimAudit.Errors(
                    SharedFoundationClaimCatalog.Claims,
                    SharedFoundationClaimCatalog.CurrentFoundationDeltaClaimIds),
                Is.Empty);

            var owner = Property<MatchSessionProjection>(nameof(MatchSessionProjection.DecisionContract));
            var inventory = Test(nameof(ConfiguredPendingChoiceProjectionTests
                .EveryChoiceProjectionMemberMapsToExactlyOneDecisionFamilyAndAnswerRows));
            var familyViolator = Test(nameof(ConfiguredPendingChoiceProjectionTests
                .AnEleventhFamilyCannotHideAsACompanionOrReuseAnOwnedProjection));

            var oneConsumerGeneral = General("probe.one-consumer", owner,
                Evidence(inventory, SharedFoundationEvidenceKind.ProductionConsumer, "same-shape"));
            AssertErrorsContain(oneConsumerGeneral, "without independent material-shape evidence");

            var equivalentNames = General("probe.equivalent-names", owner,
                Evidence(inventory, SharedFoundationEvidenceKind.ProductionConsumer, "same-shape"),
                Evidence(familyViolator, SharedFoundationEvidenceKind.ProductionConsumer, "same-shape"));
            AssertErrorsContain(equivalentNames, "without independent material-shape evidence");

            var renamedEvidence = General("probe.renamed-evidence", owner,
                Evidence(new MemberLocator(typeof(ConfiguredPendingChoiceProjectionTests),
                        "RenamedOrMissingEvidence", MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "boundary"),
                Evidence(familyViolator, SharedFoundationEvidenceKind.ViolatorFixture, "violation"));
            AssertErrorsContain(renamedEvidence, "missing or renamed evidence");

            var validGeneral = General("probe.valid-general", owner,
                Evidence(inventory, SharedFoundationEvidenceKind.BoundaryFixture, "family inventory"),
                Evidence(familyViolator, SharedFoundationEvidenceKind.ViolatorFixture,
                    "unregistered family refusal"));
            Assert.That(SharedFoundationClaimAudit.Errors(new[] { validGeneral },
                new[] { validGeneral.Id }), Is.Empty);

            var narrowWithoutScopeOrTrigger = new SharedFoundationClaim("probe.invalid-narrow", owner,
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { Evidence(inventory, SharedFoundationEvidenceKind.BoundaryFixture, "one shape") },
                new[] { "target" }, string.Empty, string.Empty);
            AssertErrorsContain(narrowWithoutScopeOrTrigger, "without an exact supported shape");
            AssertErrorsContain(narrowWithoutScopeOrTrigger,
                "without a reassessment/expansion trigger");

            var validNarrow = new SharedFoundationClaim("probe.valid-narrow", owner,
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { Evidence(inventory, SharedFoundationEvidenceKind.BoundaryFixture, "one shape") },
                new[] { "target" }, "one configured target branch",
                "a second target kind or continuation timing is introduced");
            Assert.That(SharedFoundationClaimAudit.Errors(new[] { validNarrow },
                new[] { validNarrow.Id }), Is.Empty);

            Assert.That(SharedFoundationClaimAudit.Errors(new[] { validGeneral },
                    new[] { "batch.unregistered-foundation" }),
                Has.Some.Contains("Batch delta has no registered shared-foundation claim"));
        }

        private static SharedFoundationClaim General(string id, MemberLocator owner,
            params FoundationEvidenceLocator[] evidence) => new SharedFoundationClaim(id, owner,
            SharedFoundationClaimStatus.General, evidence,
            new[] { "branch", "parameter", "timing", "target", "continuation", "refusal", "lifecycle" },
            string.Empty, "reassess when the production owner or a named shape axis widens");

        private static FoundationEvidenceLocator Evidence(MemberLocator locator,
            SharedFoundationEvidenceKind kind, string materialShape) =>
            new FoundationEvidenceLocator(locator, kind, materialShape);

        private static MemberLocator Test(string methodName) => new MemberLocator(
            typeof(ConfiguredPendingChoiceProjectionTests), methodName, MemberTypes.Method);

        private static MemberLocator Property<T>(string propertyName) =>
            new MemberLocator(typeof(T), propertyName, MemberTypes.Property);

        private static void AssertErrorsContain(SharedFoundationClaim claim, string fragment)
        {
            var errors = SharedFoundationClaimAudit.Errors(new[] { claim }, new[] { claim.Id });
            Assert.That(errors.Any(error => error.Contains(fragment)), Is.True,
                $"Expected '{fragment}', got: {string.Join(" | ", errors)}");
        }
    }

    internal static class SharedFoundationClaimCatalog
    {
        // A future batch replaces this bounded delta with its own verified new/widened foundation IDs.
        // Every ID must resolve to one catalog entry before that batch can be scheduled.
        internal static readonly string[] CurrentFoundationDeltaClaimIds =
        {
            "targeting.pre-attack-token-swap", "timing.actual-movement"
        };

        internal static readonly SharedFoundationClaim[] Claims =
        {
            new SharedFoundationClaim(
                "targeting.pre-attack-token-swap",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.EffectResolution.EffectOptionFeasibilityQuery),
                    "TryAnticipateActorSwapClause", MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetPrintedSetTests),
                    nameof(WidgetPrintedSetTests.AirborneSwapsBeforeSelectingTheAttackTarget_ThenOptionallySwapsBack), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "optional actor/token exchange before actor-relative Attack binding") },
                new[] { "origin", "optional", "feasibility", "restore" },
                "Read-only facts followed by one actor/token Swap can be judged from its legal destination or declined origin before selecting an Attack target, without applying the Swap.",
                "Reassess for a unit partner, multiple hypothetical relocations or mutating precursor; these retain the conservative unknown-position path."),
            new SharedFoundationClaim(
                "borrowing.public-skill-pools",
                new MemberLocator(typeof(GuardsDigital.Core.Cards.BorrowedPrimaryActionDeclaration),
                    nameof(GuardsDigital.Core.Cards.BorrowedPrimaryActionDeclaration.FromFaceUpSkills), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetSkillReuseTests),
                    nameof(WidgetSkillReuseTests.PoolsSeparateResolvedSkillsFromAllPublicFaceUpSkills), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "resolved-only versus public face-up owned Skill primaries") },
                new[] { "ownership", "visibility", "type", "lifecycle" },
                "Owned resolved face-up TurnArea Skills, or owned face-up TurnArea/DiscardPile Skills including the currently performed card; no hand cards or source state transition.",
                "Reassess for another public location, card type, or source-state transition."),
            new SharedFoundationClaim(
                "timing.actual-movement",
                new MemberLocator(typeof(GuardsDigital.Core.Cards.PerformedMovementAction),
                    nameof(GuardsDigital.Core.Cards.PerformedMovementAction.ForCompletedAction), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetSkillReuseTests),
                    nameof(WidgetSkillReuseTests.DragonKnightFiresAfterActualMovement_AndCanReuseTheCurrentSkill), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "zero/positive Movement end grants current Skill and restores its choices"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetPrintedSetTests),
                        nameof(WidgetPrintedSetTests.DecliningDragonKnightAfterNonBasicMovementReleasesTheHeldTurn_AfterRestore), MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture, "non-basic Movement holds Initiative without fabricating a basic repeat subject") },
                new[] { "action kind", "timing", "owner", "continuation" },
                "Actual Movement action completion including zero spaces; not direct Move, Place, Swap or Fast Travel. On-board unlocked owner only, optional Skill grant.",
                "Reassess for an in-action movement checkpoint or trigger text continuing after its grant."),
            new SharedFoundationClaim(
                "targeting.borrowed-skill-exclusion",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.Modifications.TargetQuery),
                    nameof(GuardsDigital.Core.Rules.Modifications.TargetQuery.IsPrevented), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetSkillReuseTests),
                    nameof(WidgetSkillReuseTests.ExclusionSurvivesNestedBorrow_ThenResumesItsParentAndRejectsStaleAnswers), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "Attack-granted Skill chain excludes original unit through nested choices/restore") },
                new[] { "actor", "unit identity", "grant lifetime", "immunity", "persistence" },
                "An after-Attack final borrow clause excludes the original Hero/Minion for the acting hero through its entire borrowed chain; independent of immunity and presence, allowing token-only Skills.",
                "Reassess for before-Attack borrowing, text after an Attack borrow clause, overlapping differently scoped grants, or a new operation bypassing TargetQuery."),
            new SharedFoundationClaim(
                "targeting.anchored-intersection",
                new MemberLocator(typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticAnchoredTargetCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticAnchoredTargetCapabilities.Handler), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetAnchoredSkillTests),
                    nameof(WidgetAnchoredSkillTests.BreathIntersectsWidgetRangeWithPyroLine), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "actor Range intersected with separately bound straight line") },
                new[] { "origin", "geometry", "kind", "relation", "immunity", "consumer" },
                "Widget's intrinsic Hero/Minion/Unit selection with actor-origin Range and anchor line/adjacency, or anchor-only adjacent units; typed live outputs and normal consumer feasibility.",
                "Reassess for another geometric shape, effective-kind substitution, or retaining the anchor across intervening movement."),
            new SharedFoundationClaim(
                "movement.coupled-exact-pair",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.Displacement.CoupledMoveQuery),
                    nameof(GuardsDigital.Core.Rules.Displacement.CoupledMoveQuery.CanMove), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetCoupledMoveTests),
                    nameof(WidgetCoupledMoveTests.PairFollowsVacatedSpacesInEveryDirectionAtBothPrintedDistances), MemberTypes.Method),
                    SharedFoundationEvidenceKind.BoundaryFixture, "two objects follow a common exact distance and direction") },
                new[] { "direction", "distance", "occupancy", "immunity", "atomicity", "continuation" },
                "One simultaneous two-object direct Move, both complete or neither, permitting partner-vacated transit and retaining ordinary obstacle endpoints. Widget selects exactly 2 or 3.",
                "Reassess for larger batches, unequal routes/distances, partial success or a new traversal-trigger token. Tali's one-space batch is unchanged."),
            new SharedFoundationClaim(
                "targeting.hero-space-output",
                new MemberLocator(typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName), MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(WidgetTokenOperationTests),
                    nameof(WidgetTokenOperationTests.AllAboardChecksBothRanges_AndSelectsOnlyAnotherFriendlyHero),
                    MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "printed Range hero selection feeding token/hero Swap") },
                new[] { "query", "typed output", "feasibility", "identity" },
                "Printed-icon hero selection can publish its selected hero's live space when declared by the consuming node; identity-only nodes keep their existing output.",
                "Reassess for a consumer retaining the coordinate across an intervening displacement."),
            new SharedFoundationClaim(
                "tokens.named-owned-facts",
                new MemberLocator(typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticOwnedTokenCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticOwnedTokenCapabilities.SpaceHandler), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(OwnedTokenCapabilityTests),
                    nameof(OwnedTokenCapabilityTests.PresenceAndPosition_AreOwnerAndRevisionScopedReadOnlyFacts),
                    MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "owner, definition revision and readonly outputs") },
                new[] { "query", "ownership", "reach", "feasibility" },
                "One actor-owned token from a trusted single-piece definition, optionally bounded by the source card's Range or Radius; no token selection or mutation.",
                "Reassess for multi-piece definitions, controller-based selection, non-actor anchors or hypothetical token displacement."),
            new SharedFoundationClaim(
                "tokens.explicit-round-retention",
                new MemberLocator(typeof(GuardsDigital.Core.Components.ComponentCleanupRules),
                    nameof(GuardsDigital.Core.Components.ComponentCleanupRules.TokensRemovedAtEndOfRound), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(OwnedTokenCapabilityTests),
                    nameof(OwnedTokenCapabilityTests.RetainedRoundToken_IsNotClearedByRoundCleanup_ButCanStillBeRemoved),
                    MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "mixed cleanup lifetime and later removal") },
                new[] { "lifecycle", "definition", "cleanup" },
                "A trusted definition may retain a token at round end; its identity, properties and ordinary removal paths do not change.",
                "Reassess for another automatic removal condition or a mutable per-instance lifetime."),
            new SharedFoundationClaim(
                "resolution.skill-hero-defeat-order",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.EffectResolution.EffectResolutionContext),
                    nameof(GuardsDigital.Core.Rules.EffectResolution.EffectResolutionContext.StagesHeroDefeats), MemberTypes.Property),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] { new FoundationEvidenceLocator(new MemberLocator(typeof(GarrusComposedRoundTests),
                    nameof(GarrusComposedRoundTests.HeavyPilum_CompletesDiscardOrDefeatBeforeOptionalMovement),
                    MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "Defeat before Move; restore and no duplicate life spend") },
                new[] { "resolution", "consequence", "continuation" },
                "Skill execution stages each hero-Defeat node before reading its next clause; completed frames do not request it again.",
                "Reassess for an interruptible hero-removal decision or a producer outside ordinary/repeated/borrowed Skills."),
            new SharedFoundationClaim(
                "feasibility.readonly-condition-binding",
                new MemberLocator(typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities.OwnedCardPresentV1), MemberTypes.Property),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[] {
                    new FoundationEvidenceLocator(new MemberLocator(typeof(GarrusComposedRoundTests),
                        nameof(GarrusComposedRoundTests.PursuitWithoutDiscard_CannotStartWhenBothBranchesHaveNoAdjacentTarget),
                        MemberTypes.Method), SharedFoundationEvidenceKind.ViolatorFixture, "empty discard does not invent pre-movement"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(GarrusComposedRoundTests),
                        nameof(GarrusComposedRoundTests.PursuitCards_MoveBeforeBindingHeroAndKeepThePrintedAttack),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "discard-present branch reaches target after Move") },
                new[] { "query", "condition", "target binding" },
                "An owned-card-presence predicate at a card-text branch exposes read-only facts to target-binding feasibility.",
                "Reassess for a condition following hypothetical mutation or another anticipated predicate family."),
            new SharedFoundationClaim(
                "timing.resolved-discard-primary",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.Progression.ResolvedCardDiscardTriggerQuery),
                    nameof(GuardsDigital.Core.Rules.Progression.ResolvedCardDiscardTriggerQuery.Derive), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(new MemberLocator(typeof(ResolvedDiscardTriggerTests),
                        nameof(ResolvedDiscardTriggerTests.WrongChooserAndStaleAcceptance_AreAtomic_AndCannotGrantTwice),
                        MemberTypes.Method), SharedFoundationEvidenceKind.ViolatorFixture, "chooser and replayed acceptance"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(ResolvedDiscardTriggerTests),
                        nameof(ResolvedDiscardTriggerTests.CommandsReplayAndRetainedPrefixes_ReproduceEveryPauseAndOrderedEvent),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "nested grant, snapshot and retained-prefix replay")
                }, new[] { "timing", "borrow", "continuation" },
                "The successful resolved-card discard in Skill execution interrupts before the next clause; " +
                "an on-board owner's unlocked Ultimate may grant that exact discarded primary.",
                "Reassess for a resolved-discard producer outside Skill execution or a trigger with work after its grant."),
            new SharedFoundationClaim(
                "movement.adjacent-enemy-hero-for-push",
                new MemberLocator(typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities.MoveAdjacentToEnemyHeroForPushV1),
                    MemberTypes.Property), SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(new MemberLocator(typeof(DisplacementCapabilityTests),
                        nameof(DisplacementCapabilityTests.AdjacentPushMove_RequiresAReachableEligibleHeroAndRevalidatesBeforeMutation),
                        MemberTypes.Method), SharedFoundationEvidenceKind.ViolatorFixture, "reachable adjacency and live Push immunity"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(GarrusAttackVocabularyTests),
                        nameof(GarrusAttackVocabularyTests.PostAttack_MovePauseAndNamedPush_RoundTripAndContinueIdentically),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture, "move and named-hero pauses across snapshots")
                }, new[] { "movement", "target", "continuation" },
                "Direct self Move ends beside an eligible opposing hero; zero satisfies adjacency under GA-Q1, " +
                "then ordinary selection explicitly names the Push target.",
                "Reassess for another mover, relation, target kind or following operation."),
            new SharedFoundationClaim(
                "values.owned-discard-count",
                new MemberLocator(typeof(GuardsDigital.Core.Rules.CardResolution.RosterDynamicValueQuery),
                    nameof(GuardsDigital.Core.Rules.CardResolution.RosterDynamicValueQuery.Contributions), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(new MemberLocator(typeof(RosterHistoryPresenceAndValueTests),
                        nameof(RosterHistoryPresenceAndValueTests.OwnedDiscardBonus_CountsLiveLocationRegardlessOfPlayedStateOrFace),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture,
                        "live discard/retrieval, owner isolation, played state and orientation"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(RosterHistoryPresenceAndValueTests),
                        nameof(RosterHistoryPresenceAndValueTests.OwnedDiscardBonus_ComposesWithAttackItems_AndRebindsOnRestore),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture,
                        "item contribution and pending attack restore")
                },
                new[] { "value", "owner", "card location" },
                "Attack card-text contribution counts every card currently in the acting hero's discard.",
                "Reassess for a different owner, pile, value channel or card-characteristic filter."),
            new SharedFoundationClaim(
                "card-state.resolved-discard",
                new MemberLocator(typeof(GuardsDigital.Core.Cards.CardLifecycleAuthority),
                    nameof(GuardsDigital.Core.Cards.CardLifecycleAuthority.TryDiscardResolvedCard), MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(new MemberLocator(typeof(ResolvedCardDiscardTests),
                        nameof(ResolvedCardDiscardTests.DiscardChoice_IsOwnedCanonicalAndRejectsStaleForeignOrUnresolvedCards),
                        MemberTypes.Method), SharedFoundationEvidenceKind.ViolatorFixture,
                        "owner, resolved source state and stale answer"),
                    new FoundationEvidenceLocator(new MemberLocator(typeof(ResolvedCardDiscardTests),
                        nameof(ResolvedCardDiscardTests.PendingDiscard_RoundTripsAndContinuesIdentically_WithPlayedDiscardInSnapshot),
                        MemberTypes.Method), SharedFoundationEvidenceKind.BoundaryFixture,
                        "pending choice and played-discard snapshot continuation")
                },
                new[] { "card lifecycle", "owner", "continuation" },
                "One actor-owned resolved card in the turn area becomes a played, resolved discard; " +
                "its source Active effect cancels and existing retrieval admits the state.",
                "Reassess for another owner's discard choice, unresolved discard, batch discard, or a new discard producer."),
            new SharedFoundationClaim(
                "application.pending-decision-contract",
                new MemberLocator(typeof(MatchSessionProjection),
                    nameof(MatchSessionProjection.DecisionContract), MemberTypes.Property),
                SharedFoundationClaimStatus.General,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(ConfiguredPendingChoiceProjectionTests),
                            nameof(ConfiguredPendingChoiceProjectionTests
                                .EveryChoiceProjectionMemberMapsToExactlyOneDecisionFamilyAndAnswerRows),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "ten typed families and thirty command-answer rows"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(ConfiguredPendingChoiceProjectionTests),
                            nameof(ConfiguredPendingChoiceProjectionTests
                                .AnEleventhFamilyCannotHideAsACompanionOrReuseAnOwnedProjection),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "unregistered family plus missing surface and command ownership"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(ConfiguredPendingChoiceProjectionTests),
                            nameof(ConfiguredPendingChoiceProjectionTests
                                .EveryConfiguredChoiceAnswerPublishesTheCurrentProjectionOrCommitsNothing),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "configured acceptance, immutable projection publication, and stale refusal")
                },
                new[]
                {
                    "decision family", "answer command shape", "visible surface",
                    "authority acceptance/refusal", "projection lifecycle"
                },
                string.Empty,
                "reassess when a pending family, answer shape, interactive surface, driver, or " +
                "projection lifecycle is added or widened"),

            // Milestone 8.57a. Two OUTPUTS added to capabilities this build already registers, each
            // publishing a fact its own handler already answered - see each claim's supported shape.
            new SharedFoundationClaim(
                "consequence.attack-target-identity",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities
                        .AttackTargetHeroOutput),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaRewardTests),
                            nameof(RowennaRewardTests
                                .CodeOfChivalry_AgainstAHero_OffersEachHeroTheirOwnPileAndNobodyElses),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "step-1 rider names the enclosing Attack's hero target as a step's subject"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaRewardTests),
                            nameof(RowennaRewardTests
                                .CodeOfChivalry_AgainstAMinion_OffersNeitherRetrieval),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "minion target publishes no hero and the dependent step is not reached")
                },
                new[] { "enclosing action", "target kind", "publication", "timing" },
                "The enclosing Attack's hero target, published by the predicate that already reports " +
                "whether one exists, only for a node declaring the output and only when the target " +
                "is a hero.",
                "Reassess for a minion target named as a step's subject, for text outside an " +
                "enclosing Attack, or for a second unit an Attack could take."),
            new SharedFoundationClaim(
                "resources.grant-performed-flag",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities
                        .DidGrantOutputName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaRewardTests),
                            nameof(RowennaRewardTests
                                .ParagonOfGrace_AsksForOneRepeatOnADifferentTarget_OnceTheCoinIsPaid),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a credited coin gates a repeat that binds nothing from the grant"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaRewardTests),
                            nameof(RowennaRewardTests
                                .ParagonOfGrace_AsksForNoRepeatWhenNoFriendlyHeroIsInRadius),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "an ungranted coin leaves the flag unbound and the gated step unoffered")
                },
                new[] { "grant", "condition", "publication", "continuation" },
                "Whether a positive coin grant was committed, published only on success and only " +
                "for a node declaring the output; transfers and p.13's reward schedule are unchanged.",
                "Reassess for a spend, a transfer, or a condition that needs the amount rather than " +
                "the fact.")
        };
    }

    internal enum SharedFoundationClaimStatus
    {
        General,
        ExplicitlyNarrow
    }

    internal enum SharedFoundationEvidenceKind
    {
        ProductionConsumer,
        BoundaryFixture,
        ViolatorFixture
    }

    internal sealed class SharedFoundationClaim
    {
        internal SharedFoundationClaim(string id, MemberLocator owner,
            SharedFoundationClaimStatus status, IEnumerable<FoundationEvidenceLocator> evidence,
            IEnumerable<string> shapeAxes, string supportedShape, string expansionTrigger)
        {
            Id = id;
            Owner = owner;
            Status = status;
            Evidence = Array.AsReadOnly((evidence ?? Array.Empty<FoundationEvidenceLocator>()).ToArray());
            ShapeAxes = Array.AsReadOnly((shapeAxes ?? Array.Empty<string>()).ToArray());
            SupportedShape = supportedShape;
            ExpansionTrigger = expansionTrigger;
        }

        internal string Id { get; }
        internal MemberLocator Owner { get; }
        internal SharedFoundationClaimStatus Status { get; }
        internal IReadOnlyList<FoundationEvidenceLocator> Evidence { get; }
        internal IReadOnlyList<string> ShapeAxes { get; }
        internal string SupportedShape { get; }
        internal string ExpansionTrigger { get; }
    }

    internal sealed class FoundationEvidenceLocator
    {
        internal FoundationEvidenceLocator(MemberLocator locator, SharedFoundationEvidenceKind kind,
            string materialShape)
        {
            Locator = locator;
            Kind = kind;
            MaterialShape = materialShape;
        }

        internal MemberLocator Locator { get; }
        internal SharedFoundationEvidenceKind Kind { get; }
        internal string MaterialShape { get; }
    }

    internal sealed class MemberLocator
    {
        internal MemberLocator(Type declaringType, string memberName, MemberTypes memberType)
        {
            DeclaringType = declaringType;
            MemberName = memberName;
            MemberType = memberType;
        }

        internal Type DeclaringType { get; }
        internal string MemberName { get; }
        internal MemberTypes MemberType { get; }

        internal MemberInfo Resolve()
        {
            if (DeclaringType == null || string.IsNullOrWhiteSpace(MemberName)) return null;
            return DeclaringType.GetMember(MemberName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly)
                .SingleOrDefault(member => member.MemberType == MemberType);
        }

        public override string ToString() =>
            (DeclaringType?.FullName ?? "<missing type>") + "." + (MemberName ?? "<missing member>");
    }

    internal static class SharedFoundationClaimAudit
    {
        internal static IReadOnlyList<string> Errors(IEnumerable<SharedFoundationClaim> claims,
            IEnumerable<string> requiredClaimIds)
        {
            var errors = new List<string>();
            var entries = (claims ?? Array.Empty<SharedFoundationClaim>()).ToArray();
            var required = (requiredClaimIds ?? Array.Empty<string>()).ToArray();

            foreach (var requiredId in required.Distinct(StringComparer.Ordinal))
            {
                if (!entries.Any(claim => claim != null &&
                                          string.Equals(claim.Id, requiredId, StringComparison.Ordinal)))
                    errors.Add("Batch delta has no registered shared-foundation claim: " + requiredId);
            }

            foreach (var duplicate in entries.Where(claim => claim != null)
                         .GroupBy(claim => claim.Id, StringComparer.Ordinal)
                         .Where(group => group.Count() != 1))
                errors.Add("Shared-foundation claim ID is not unique: " + duplicate.Key);

            foreach (var claim in entries)
                AuditClaim(claim, errors);

            return errors;
        }

        private static void AuditClaim(SharedFoundationClaim claim, ICollection<string> errors)
        {
            if (claim == null)
            {
                errors.Add("Shared-foundation catalog contains a null claim.");
                return;
            }

            if (string.IsNullOrWhiteSpace(claim.Id))
                errors.Add("Shared-foundation claim has no stable ID.");
            if (claim.Owner?.Resolve() == null)
                errors.Add(claim.Id + " has a missing or renamed production owner: " + claim.Owner);
            else if ((claim.Owner.DeclaringType.Namespace ?? string.Empty)
                     .StartsWith("GuardsDigital.Tests", StringComparison.Ordinal))
                errors.Add(claim.Id + " names a test member as its production owner: " + claim.Owner);
            if (!Enum.IsDefined(typeof(SharedFoundationClaimStatus), claim.Status))
                errors.Add(claim.Id + " has an unknown claim status.");
            if (claim.ShapeAxes.Count == 0 || claim.ShapeAxes.Any(string.IsNullOrWhiteSpace))
                errors.Add(claim.Id + " does not state its relevant shape axes.");
            if (string.IsNullOrWhiteSpace(claim.ExpansionTrigger))
                errors.Add(claim.Id + " has no reassessment/expansion trigger.");
            if (claim.Evidence.Count == 0)
                errors.Add(claim.Id + " has no exact evidence locators.");

            foreach (var evidence in claim.Evidence)
            {
                if (evidence?.Locator?.Resolve() == null)
                    errors.Add(claim.Id + " has missing or renamed evidence: " + evidence?.Locator);
                if (string.IsNullOrWhiteSpace(evidence?.MaterialShape))
                    errors.Add(claim.Id + " has evidence with no material shape.");
                if (evidence?.Kind == SharedFoundationEvidenceKind.ProductionConsumer &&
                    (evidence.Locator?.DeclaringType?.Namespace ?? string.Empty)
                    .StartsWith("GuardsDigital.Tests", StringComparison.Ordinal))
                    errors.Add(claim.Id + " labels test evidence as a production consumer: " +
                               evidence.Locator);
            }

            if (claim.Status == SharedFoundationClaimStatus.General)
            {
                var distinctEvidence = claim.Evidence
                    .Where(evidence => evidence != null)
                    .Where(evidence => !string.IsNullOrWhiteSpace(evidence.MaterialShape))
                    .GroupBy(evidence => evidence.MaterialShape, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToArray();
                var productionConsumerShapes = distinctEvidence.Count(evidence =>
                    evidence.Kind == SharedFoundationEvidenceKind.ProductionConsumer);
                var hasBoundaryShape = distinctEvidence.Any(evidence =>
                    evidence.Kind == SharedFoundationEvidenceKind.BoundaryFixture);
                var hasViolatorShape = distinctEvidence.Any(evidence =>
                    evidence.Kind == SharedFoundationEvidenceKind.ViolatorFixture);
                if (productionConsumerShapes < 2 && !(hasBoundaryShape && hasViolatorShape))
                    errors.Add(claim.Id + " is general without independent material-shape evidence.");
                return;
            }

            if (string.IsNullOrWhiteSpace(claim.SupportedShape))
                errors.Add(claim.Id + " is narrow without an exact supported shape.");
            if (string.IsNullOrWhiteSpace(claim.ExpansionTrigger))
                errors.Add(claim.Id + " is narrow without a reassessment/expansion trigger.");
            if (ContainsBroadScopeWord(claim.SupportedShape))
                errors.Add(claim.Id + " is narrow but uses general-scope wording.");
        }

        private static bool ContainsBroadScopeWord(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var words = value.Split(new[] { ' ', '-', '/', ',', '.', ':', ';', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries);
            return words.Any(word => string.Equals(word, "all", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(word, "any", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(word, "general", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(word, "universal", StringComparison.OrdinalIgnoreCase));
        }
    }
}

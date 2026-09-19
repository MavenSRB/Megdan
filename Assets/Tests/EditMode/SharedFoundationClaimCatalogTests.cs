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
                "the fact."),

            // Milestone 8.57b. One PARAMETER on a printed-icon selection arm and one parameter plus
            // one OUTPUT on the direct Move, each widening a foundation this build already registers.
            new SharedFoundationClaim(
                "targeting.unit-swap-qualifier",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities
                        .AdjacentToAnEnemyHeroOrHoldingADiscardParameterName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .StandGuard_OffersExactlyTheQualifyingFriendlyUnitsInsideItsPrintedRange),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "one printed clause admits a hero by its pile and a minion by its neighbour"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .StandGuard_LeavesOutAFriendlyHeroWhoseDiscardIsEmptyAndWhoStandsBesideNobody),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a candidate satisfying neither half of the disjunction is not offered")
                },
                new[] { "selection arm", "disjunction", "candidate kind", "reach" },
                "One optional clause on the printed-icon UNIT arm: a candidate adjacent to an enemy " +
                "hero of the actor's, OR a hero holding a card in their own discard. It narrows who " +
                "may be picked and nothing else, and it is unbound on every other arm.",
                "Reassess for a face pairing the clause with a Radius icon, for a qualifier measured " +
                "from anywhere but the candidate's own space, or for a sentence that joins two " +
                "qualifiers with 'and'."),
            new SharedFoundationClaim(
                "displacement.move-chooser",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities
                        .ChooserParameterName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .ThrowTheGauntlet_PutsTheMoveToTheNamedHeroAndRefusesTheActorAnsweringIt),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a named hero owns the pause and the acting seat is the wrong chooser"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .ThrowTheGauntlet_AsksNobodyAndPaysNothingWhenTheNamedHeroCannotMove),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a mover with no destination opens no prompt for anybody to own")
                },
                new[] { "decision owner", "mover", "declaration", "failure policy" },
                "Which hero answers a card-text Move, where a definition names one; unbound, the " +
                "decision is the actor's exactly as it has been since milestone 6.4. It changes who " +
                "is asked and never what the Move may do.",
                "Reassess for a mover-chooser who is not the mover, for a Movement ACTION rather " +
                "than a card-text Move, or for a Push whose distance a second hero would choose."),
            new SharedFoundationClaim(
                "displacement.move-performed-flag",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities
                        .DidMoveOutputName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .ThrowTheGauntlet_PaysHerTwoCoinsWhenTheNamedHeroMoves),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a committed move gates a grant that binds nothing from the mover"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSwapAndGauntletTests),
                            nameof(RowennaSwapAndGauntletTests
                                .ThrowTheGauntlet_PaysNothingWhenTheNamedHeroDeclines),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a declined move leaves the flag unbound and the gated step unoffered")
                },
                new[] { "move", "condition", "publication", "continuation" },
                "Whether a card-text Move was committed, published only on success and only for a " +
                "node declaring the output - 8.40i's didRetrieve and 8.57a's didGrant for a Move.",
                "Reassess for a Push, a Place, or a condition that needs how far rather than whether."),

            // Milestone 8.57c. One PARAMETER on the written-distance selection and one parameter plus
            // one OUTPUT on the Defeat, each widening a foundation this build already registers.
            new SharedFoundationClaim(
                "targeting.empty-hand-narrowing",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities
                        .RequiresEmptyHandParameterName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSurrenderTests),
                            nameof(RowennaSurrenderTests
                                .AcceptSurrender_AdmitsAHeroWhoseOnlyCardsArePlayedResolvedOrDiscarded),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a hero whose whole hand has been played or discarded holds nothing"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSurrenderTests),
                            nameof(RowennaSurrenderTests
                                .AcceptSurrender_LeavesOutAnAdjacentEnemyHoldingASingleCard),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "one card in hand is not no cards, and the candidate is dropped")
                },
                new[] { "selection arm", "hand", "candidate kind", "offer feasibility" },
                "One optional flag on the written-distance hero selection: a candidate holding zero " +
                "cards in its own hand, read through HeroHandQuery as milestone 8.40f's count form " +
                "reads it. It narrows who may be picked and nothing else, and it is unbound on every " +
                "printed-icon arm.",
                "Reassess for a face printing the clause beside a Range or Radius icon, for a clause " +
                "about a hand size other than zero, or for a sentence that reads a hand's COLOURS " +
                "rather than its size."),
            new SharedFoundationClaim(
                "consequences.assist-coin-multiplier",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities
                        .AssistCoinMultiplierParameter),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSurrenderTests),
                            nameof(RowennaSurrenderTests
                                .GloriousTriumph_TriplesTheAssistCoinsAndNothingElse),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "one printed clause multiplies one column of p.13's table for one Defeat"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaSurrenderTests),
                            nameof(RowennaSurrenderTests
                                .ThePageFourteenAttackEntrance_StillPaysThePagesOwnAssistCoins),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "the entrance that carries no clause is priced by the page alone")
                },
                new[] { "defeat request", "assist coins", "spend site", "entrance" },
                "One optional integer on the Defeat request, multiplying rulebook p.13's ASSIST coins " +
                "for that one Defeat where a face prints a clause; the Defeat coins and the Life " +
                "counters are two other sentences and do not move. Unbound, it is the page's own " +
                "schedule, which is what both entrances of the spend site read today.",
                "Reassess for a clause that multiplies the Defeat coins or the Life counters, for one " +
                "that reduces a column rather than multiplying it, or for a standing rule about every " +
                "Defeat rather than about one - which is a RuleModificationKind and not this."),

            // Milestone 8.57d. One PARAMETER on the printed-Radius hero selection, widening a
            // foundation this build already registers. The milestone's other delta is a further
            // DECLARATION over a registered handler type, which this catalog does not track: nothing
            // shared changes shape for it.
            new SharedFoundationClaim(
                "targeting.both-discard-piles-empty",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities
                        .BothDiscardPilesEmptyParameterName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaCloseQuartersTests),
                            nameof(RowennaCloseQuartersTests
                                .OpeningShots_OffersExactlyTheEnemyHeroesWhoseDiscardIsEmpty),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "one printed clause reads two heroes' piles and admits only the empty pair"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(RowennaCloseQuartersTests),
                            nameof(RowennaCloseQuartersTests
                                .OpeningShots_HasNoLegalTargetAtAllWhileHerOwnDiscardHoldsACard),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a card in the ACTOR's own pile empties the whole candidate list")
                },
                new[] { "selection arm", "discard pile", "subject count", "offer feasibility" },
                "One optional flag on the printed-Radius hero selection: the acting hero's own discard " +
                "pile AND the candidate's own are both empty, read through HeroDiscardPileQuery as " +
                "hasCardInDiscard reads the positive form. It narrows who may be picked and nothing " +
                "else, it is unbound on every other arm, and the actor's half is asked once for the " +
                "whole offer rather than per candidate.",
                "Reassess for a face pairing the clause with a Range icon, for a clause about a third " +
                "party's pile, or for one that reads how MANY cards a pile holds rather than whether " +
                "it holds any.")
            ,

            // Milestone 8.61a. One NEW production member on p.19's straight-line owner, plus the one
            // new reading of it at p.19's Target owner. Min's Smoke Bomb is the first printed rule in
            // this project that answers about a PAIR of positions rather than about an object, so the
            // geometry is stated once, beside the rule it reads, and consulted from one place.
            new SharedFoundationClaim(
                "targeting.token-screened-straight-line",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Board.StraightLineQuery),
                    nameof(GuardsDigital.Core.Board.StraightLineQuery.LiesStrictlyBetween),
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinSmokeBombTests),
                            nameof(MinSmokeBombTests
                                .TheScreenHoldsOnEverySixAxisWithTheBombStrictlyBetween),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "six files measured with the token one or two spaces along and the unit two " +
                        "or three"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinSmokeBombTests),
                            nameof(MinSmokeBombTests.AnAdjacentPairIsNeverScreened),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a separation of one space admits no token between it, from either side"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinSmokeBombTests),
                            nameof(MinSmokeBombTests
                                .NoScreenReachesAnActingMinionOrASystemLevelResolutionThatNamesNoActor),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "an acting minion and a resolution naming no acting hero pass untouched")
                },
                new[] { "geometry", "actor", "target kind", "side", "lifetime" },
                "One placed token bars the opponents of its controller from targeting a hero or a " +
                "minion while its own space lies strictly between the two on one of p.19's six " +
                "files, read once at TargetQuery.IsPrevented and re-derived per command from live " +
                "occupancy; presence questions, a token target, an acting minion, a resolution " +
                "naming no acting hero and the controller's own side are untouched.",
                "Reassess for a screen measured from spaces other than the two units' own, for one " +
                "shaped as a disk rather than a file, for a second token definition declaring this " +
                "kind, or for a face that screens something p.19 does not call a unit.")
            ,

            // Milestone 8.61g. A NEW capability and a widening of the seam that applies rulebook
            // p.14's card text: Min's two Gold faces apply the "After the attack" text of another card
            // of hers inside their own Attack.
            new SharedFoundationClaim(
                "borrowing.after-attack-card-text",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Cards.BorrowedCardTextDeclaration),
                    nameof(GuardsDigital.Core.Cards.BorrowedCardTextDeclaration
                        .FromResolvedOrDiscardedCards),
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinBorrowedRiderTests),
                            nameof(MinBorrowedRiderTests.ThePool_IsHerRedCard_OnlyWhileItIsResolvedOrDiscarded),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "owned card resolved or discarded supplies the rider; neither state does not"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinBorrowedRiderTests),
                            nameof(MinBorrowedRiderTests
                                .ThePool_RefusesAnUltimate_AParkedHandicap_AndAFaceWithNoAfterAttackText),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "no printed primary, deferred text, or no step-7 body leaves the pool empty")
                },
                new[] { "pool", "ownership", "colour", "moment", "composition" },
                "One owned card of a named colour, resting resolved in the turn area or in the discard " +
                "pile, whose printed primary carries a composed body at rulebook p.14's step 7; the " +
                "card asking is excluded and the borrowed card's state is untouched.",
                "Reassess for a clause naming another hero's card, a pool admitting a card in hand or " +
                "committed-unresolved, or a borrow of card text at p.14's step 1 or step 4."),
            new SharedFoundationClaim(
                "resolution.borrowed-after-attack-splice",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Rules.CardResolution.CardActionResolutionAuthority),
                    "AppendBorrowedAfterAttackText",
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinBorrowedRiderTests),
                            nameof(MinBorrowedRiderTests
                                .TheBorrowedReach_IsTheRedCardsPrintedRadius_AndNeverFastAsLightnings),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "the borrowed body reads the borrowed card's Radius icon with p.16's items"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinBorrowedRiderTests),
                            nameof(MinBorrowedRiderTests
                                .APausePausedInsideABorrowedBody_RoundTripsThroughASnapshotAndStillOffersWhatItOffered),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a pause inside the borrowed frame restores and still offers its candidates"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinBorrowedRiderTests),
                            nameof(MinBorrowedRiderTests
                                .TheRiderAppliesNothing_AndTheAttackIsUnaffected_WhenTheRedCardIsNeither),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "an unrequested borrow splices nothing and the attack still completes once")
                },
                new[] { "moment", "source card", "remainder", "continuation", "persistence" },
                "One further p.14 step-7 card-text step, named by a completed step-7 body's own borrow " +
                "node, carrying the same attack and outcome as its remainder; the borrowed card is the " +
                "frame's source, so its printed values and icons are what the body reads, and no " +
                "second AttackResolutionCompleted or ActionEnded is produced.",
                "Reassess for a borrow read at p.14's step 1 or step 4, for a body with work after its " +
                "borrow node, or for a borrowed body that would itself borrow.")
            ,

            // Milestone 8.61f (merged after 8.61a and 8.61g).
            new SharedFoundationClaim(
                "targeting.anchored-hero-exclusion",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticAnchoredTargetCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticAnchoredTargetCapabilities
                        .ExcludedHeroParameterName),
                    MemberTypes.Field),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinGrenadeTests),
                            nameof(MinGrenadeTests
                                .TheAnchoredSelectionsExclusionIsCarriedOnlyByAnInstanceThatDeclaresIt),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "optional named-hero exclusion declared per instance, absent from the four " +
                        "registered anchored selections"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinGrenadeTests),
                            nameof(MinGrenadeTests
                                .HolysSecondPassOffersEverybodyButTheHeroTheFirstPassNamed),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a second pass over one anchor excluding the hero its first pass named")
                },
                new[] { "declaration", "parameter", "binding", "unit kind", "printed count" },
                "One optional hero parameter on an anchor-only adjacent hero selection, declared by " +
                "an instance constructed to read it and unbound on every other: it drops one named " +
                "hero from the candidates and narrows nothing else. A minion-kind selection " +
                "declaring it is refused, and an unbound binding - a declined first pass - excludes " +
                "nobody.",
                "Reassess for a third pass, for an exclusion of a minion or a space, for one read " +
                "from something other than a previous pass's own published hero, or for a printed " +
                "count above two.")
            ,

            // Milestone 8.61h (merged after 8.61a, 8.61g, 8.61f and 8.61d).
            new SharedFoundationClaim(
                "values.item-bonus-multiplier",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Rules.Modifications.RuleModification),
                    nameof(GuardsDigital.Core.Rules.Modifications.RuleModification
                        .ForItemBonusMultiplier),
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .TheDoubling_ReachesEveryChannelPageSixteenNames),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "one item adds two points on each of p.16's six icons at once"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .TheDoubling_AddsNothingToACardPrintingNoMatchingIcon),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "p.16's matching-icon condition still refuses before the multiplier is read")
                },
                new[] { "value channel", "owner", "lifetime", "projection" },
                "One multiplier on p.16's per-item amount, declared about one hero by a ThisRound " +
                "Active effect scoped to its own controller, read wherever that bonus is composed. " +
                "Several declarations take the largest; the matching-icon condition is unchanged.",
                "Reassess for a declaration scoped to somebody other than its controller, for one " +
                "qualified to a single icon, or for a printed face that reduces an item bonus."),
            new SharedFoundationClaim(
                "timing.attack-action",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Cards.PerformedAttackAction),
                    nameof(GuardsDigital.Core.Cards.PerformedAttackAction.ForCompletedAction),
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .FlurryOfBlows_FiresAfterABasicAttackAndAfterAColouredOne),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "a completed Attack off a basic card and off a coloured one both reach it"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .ThePerformedAttackFact_IsProducedForAnAttackAndForNoOtherKind),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "every other action kind, p.13's Clear and p.11's Fast Travel included, " +
                        "records nothing")
                },
                new[] { "action kind", "card supertype", "repeat", "owner" },
                "One completed Attack action of the acting hero's, whatever card it came off, " +
                "carrying whether that instance was itself a repeat. It feeds one trigger condition " +
                "and one repeat subject; p.10's no-second-repeat is read off the fact rather than " +
                "suppressing it.",
                "Reassess for a condition that reads what the Attack DID rather than that it " +
                "happened, or for a second consumer of the fact beside the after-attack trigger."),
            new SharedFoundationClaim(
                "progression.deck-card-as-item",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities),
                    nameof(GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities
                        .TakeDeckCardAsItemV1),
                    MemberTypes.Property),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .PerfectSelf_TakingOnlyTheDeckCard_MakesItAPermanentItemUnderItsOwnIcon),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "one chosen deck card becomes an item under the slot its own icon matches"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinItemAndFlurryTests),
                            nameof(MinItemAndFlurryTests
                                .PerfectSelf_RefusesAnAnswerNamingACardThatHasLeftTheDeck),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "an answer re-derived against the live deck refuses and mutates nothing")
                },
                new[] { "card location", "tier", "owner", "item slot", "continuation" },
                "One card the acting hero owns, in her deck, printing the declared Tier and an Item " +
                "icon her dashboard accepts, chosen by her and placed as a permanent item through " +
                "p.15's set-aside state. It moves no Level, spends no coin and asks nobody else.",
                "Reassess for a source zone other than the deck, for a chooser other than the card's " +
                "owner, or for a face that names the icon rather than reading the card's own.")
            ,

            // Milestone 8.61c (merged after wave 1).
            new SharedFoundationClaim(
                "displacement.move-beside-a-named-space",
                new MemberLocator(
                    typeof(GuardsDigital.Core.Rules.Displacement.DirectMoveDestinations),
                    nameof(GuardsDigital.Core.Rules.Displacement.DirectMoveDestinations.Admit),
                    MemberTypes.Method),
                SharedFoundationClaimStatus.ExplicitlyNarrow,
                new[]
                {
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinStanceTests),
                            nameof(MinStanceTests
                                .TheNewMovePolicyAdmitsTheRingAroundABoundSpaceAndNothingWithoutOne),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.BoundaryFixture,
                        "the ring at a distance of exactly one from a bound space, filtered out of a " +
                        "reachable set rather than produced from the map"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinStanceTests),
                            nameof(MinStanceTests
                                .TigersMoveOffersOnlyTheSpacesBesideTheTargetSheCanWalkToInOneStep),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a space beside the named unit that no legal walk reaches is refused, which " +
                        "is what keeps this a p.11 Move rather than a p.18 Place"),
                    new FoundationEvidenceLocator(
                        new MemberLocator(typeof(MinStanceTests),
                            nameof(MinStanceTests
                                .WithTheTargetRemovedTigersMoveIsNotOfferedAtAllAndThePushStillFollows),
                            MemberTypes.Method),
                        SharedFoundationEvidenceKind.ViolatorFixture,
                        "a referent p.14 step 6 removed admits nothing rather than everything")
                },
                new[] { "geometry", "binding", "reachability", "distance", "refusal" },
                "One card-text direct Move whose destination must stand at a distance of exactly one " +
                "from a coordinate an earlier node published, intersected with the Movement action's " +
                "own reachability at the declared distance and refusing everything when the " +
                "coordinate is unbound; it measures from no actor, crosses no obstacle and names no " +
                "destination of its own.",
                "Reassess for a clause bounding a move by a RING wider than adjacency, for one " +
                "measured from a piece rather than from a space, for a second bound space in one " +
                "declaration, or for a face pairing it with obstacle crossing.")
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

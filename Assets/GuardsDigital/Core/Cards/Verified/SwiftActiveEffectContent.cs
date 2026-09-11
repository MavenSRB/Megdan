using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The five Active effects Swift the Sharpshooter's faces create - milestone 8.38d.
    ///
    /// <para><b>Two are End-of-turn placements and three are standing promises, and the second group
    /// is a shape no hero before her has printed.</b> Delayed Jump and Mobile Scout are Silverarrow's
    /// "End of turn:" lifecycle exactly - <see cref="ActiveEffectDurationKind.EndOfTurn"/>, created
    /// when the Skill resolves and triggered at <see cref="TimingCheckpoint.TurnEnded"/>. Mark for
    /// Death and Hunting Season create something different: a promise that pays when something happens
    /// to a THIRD party, during a window that has not opened yet.</para>
    ///
    /// <para><b>The promises are counted by multiplicity, not by a counter.</b> Mark for Death stands
    /// one and Hunting Season two, and milestone 8.38c1's
    /// <c>MinionDefeatBountyQuery</c> spends exactly one per minion Defeat - so "the first two times"
    /// is answered by how many are still live. The two Hunting Season definitions are therefore two
    /// declarations with two uniqueness keys rather than one used twice: the lifecycle suppresses a
    /// duplicate, and a shared key would silently turn Hunting Season into Mark for Death. A case in
    /// <c>MinionDefeatBountyTests</c> asserts that suppression, so this is written against a stated
    /// rule rather than against a surprise.</para>
    ///
    /// <para><b>None of the three declares a radius</b>, and that is the type's own rule rather than an
    /// omission: an Active effect may declare one only when its own SCOPE is measured from a disk, and
    /// a promise scoped to its controller measures nothing. The disk a bounty is paid over is read from
    /// the source card's printed Radius at the moment of the Defeat, which is also what lets p.16's
    /// Radius item widen it.</para>
    ///
    /// <para>Each definition is Swift's own rather than a reference to another hero's, for the reason
    /// every verified set states: a hero's content owns its own declarations, and two cards that happen
    /// to print one sentence today are two cards.</para>
    /// </summary>
    public static class SwiftActiveEffectContent
    {
        /// <summary>
        /// p.17's two ways an Active effect ends other than by its own duration - the same pair every
        /// verified hero declares.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// A bounty's three ways to end: p.17's two, plus the explicit cancellation that SPENDING it
        /// is. Milestone 8.38c1's <c>ActiveEffectLifecycle.CancelOne</c> ends one promise at
        /// <see cref="TimingCheckpoint.ExplicitCancellation"/>, and a definition that did not declare
        /// <see cref="ActiveEffectCancellationCondition.Explicit"/> could not be spent at all.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] BountyCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved,
            ActiveEffectCancellationCondition.Explicit
        };

        public static EffectCapabilityId DelayedJumpAtEndOfTurn { get; } =
            new EffectCapabilityId("swift-delayed-jump-at-end-of-turn");

        public static EffectCapabilityId MobileScoutAtEndOfTurn { get; } =
            new EffectCapabilityId("swift-mobile-scout-at-end-of-turn");

        public static EffectCapabilityId MarkForDeathBounty { get; } =
            new EffectCapabilityId("swift-mark-for-death-bounty");

        public static EffectCapabilityId HuntingSeasonFirstBounty { get; } =
            new EffectCapabilityId("swift-hunting-season-first-bounty");

        public static EffectCapabilityId HuntingSeasonSecondBounty { get; } =
            new EffectCapabilityId("swift-hunting-season-second-bounty");

        public static EffectCapabilityReference DelayedJumpAtEndOfTurnV1 =>
            new EffectCapabilityReference(DelayedJumpAtEndOfTurn, 1);

        public static EffectCapabilityReference MobileScoutAtEndOfTurnV1 =>
            new EffectCapabilityReference(MobileScoutAtEndOfTurn, 1);

        public static EffectCapabilityReference MarkForDeathBountyV1 =>
            new EffectCapabilityReference(MarkForDeathBounty, 1);

        public static EffectCapabilityReference HuntingSeasonFirstBountyV1 =>
            new EffectCapabilityReference(HuntingSeasonFirstBounty, 1);

        public static EffectCapabilityReference HuntingSeasonSecondBountyV1 =>
            new EffectCapabilityReference(HuntingSeasonSecondBounty, 1);

        /// <summary>
        /// "<b>End of turn:</b> Place yourself into a space in radius not in a straight line from you."
        /// - Delayed Jump.
        ///
        /// <para>The destination is milestone 8.38b's off-file policy over the card's own printed
        /// Radius. Note the consequence p.19 imposes that the printed words do not state: an adjacent
        /// space IS a straight line, so every one of her own neighbours is excluded and the nearest
        /// space this can reach is two away.</para>
        /// </summary>
        public static ActiveEffectDefinition DelayedJumpDefinition { get; } =
            EndOfTurnDefinition(
                "swift-delayed-jump-effect",
                "SwiftDelayedJump",
                "swift-delayed-jump",
                SwiftTriggeredEffects.DelayedJump.Reference);

        /// <summary>
        /// Delayed Jump's sentence followed by "You may then fast travel once, if able." - Mobile
        /// Scout. The Fast Travel is p.11's, invoked from card text through <c>fast-travel@1</c>, and
        /// it is optional where the page leaves her able and offers nothing where it does not.
        /// </summary>
        public static ActiveEffectDefinition MobileScoutDefinition { get; } =
            EndOfTurnDefinition(
                "swift-mobile-scout-effect",
                "SwiftMobileScout",
                "swift-mobile-scout",
                SwiftTriggeredEffects.MobileScout.Reference);

        /// <summary>
        /// "<b>Next turn:</b> the first time an enemy minion in radius is defeated, gain 1 coin." -
        /// Mark for Death's one promise.
        /// </summary>
        public static ActiveEffectDefinition MarkForDeathBountyDefinition { get; } =
            BountyDefinition(
                "swift-mark-for-death-bounty-effect", "swift-mark-for-death-bounty");

        /// <summary>Hunting Season's first promise - "The first two times ...".</summary>
        public static ActiveEffectDefinition HuntingSeasonFirstBountyDefinition { get; } =
            BountyDefinition(
                "swift-hunting-season-first-bounty-effect", "swift-hunting-season-bounty-1");

        /// <summary>
        /// Hunting Season's second promise. Its uniqueness key differs from the first's, which is the
        /// whole reason both stand: the lifecycle suppresses a duplicate, so one key would leave her
        /// with a single promise and a card that pays half what it prints.
        /// </summary>
        public static ActiveEffectDefinition HuntingSeasonSecondBountyDefinition { get; } =
            BountyDefinition(
                "swift-hunting-season-second-bounty-effect", "swift-hunting-season-bounty-2");

        /// <summary>
        /// The bodies the two End-of-turn effects run when <see cref="TimingCheckpoint.TurnEnded"/>
        /// reaches them.
        /// </summary>
        public static class SwiftTriggeredEffects
        {
            public static EffectDefinition DelayedJump { get; } =
                CreateLatePlacement(
                    new EffectDefinitionId("swift-delayed-jump-triggered"), travels: false);

            public static EffectDefinition MobileScout { get; } =
                CreateLatePlacement(
                    new EffectDefinitionId("swift-mobile-scout-triggered"), travels: true);

            public static IReadOnlyList<EffectDefinition> All { get; } = Array.AsReadOnly(new[]
            {
                DelayedJump, MobileScout
            });

            private static EffectDefinition CreateLatePlacement(
                EffectDefinitionId definitionId, bool travels)
            {
                var root = new EffectNodeId("late-root");
                var actorSpace = new EffectNodeId("late-actor-space");
                var place = new EffectNodeId("late-place");
                var travel = new EffectNodeId("late-travel");

                var children = travels
                    ? new[] { actorSpace, place, travel }
                    : new[] { actorSpace, place };

                var nodes = new List<EffectNodeDefinition>
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: children),
                    new EffectNodeDefinition(
                        actorSpace, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("space", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(
                        place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", actorSpace, "space", EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.GamePiece),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                PlaceSpawnPointExclusion.None)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("placed", EffectValueKind.Coordinate)
                        })
                };

                if (travels)
                {
                    nodes.Add(new EffectNodeDefinition(
                        travel, EffectNodeKind.Capability, EffectFailurePolicy.May,
                        SyntheticRosterCapabilities.FastTravelV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.ZoneScopeParameter,
                                FastTravelZoneScope.SameOrAdjacentZone)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("destination", EffectValueKind.Coordinate)
                        }));
                }

                return new EffectDefinition(
                    definitionId, SwiftCardContent.ContentRevision, root, nodes);
            }
        }

        private static ActiveEffectDefinition EndOfTurnDefinition(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            EffectDefinitionReference triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: triggeredEffect);

        /// <summary>
        /// One standing promise. Its <c>effectKind</c> is
        /// <see cref="MinionDefeatBountyQuery.EffectKind"/>, which is the whole coupling between the
        /// card that makes the promise and the Defeat that spends it - no card id, no hero id and no
        /// capability name crosses that seam.
        ///
        /// <para>It carries NO triggered effect: a bounty is not run at a checkpoint, it is spent at
        /// p.13's Defeat by the reward stage that already pays for one. And it declares no radius,
        /// because its scope is its controller and the disk is read from the source card.</para>
        ///
        /// <para>It carries across the round boundary, because "Next turn" may be a turn in the
        /// following round - a hero who plays this on their last turn of a round is promised the next
        /// one, and p.6 does not empty that promise with the turn area.</para>
        /// </summary>
        private static ActiveEffectDefinition BountyDefinition(
            string definitionId, string uniquenessKey) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                BountyCancellations,
                true,
                MinionDefeatBountyQuery.EffectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>());

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(DelayedJumpAtEndOfTurnV1, DelayedJumpDefinition),
            new ActiveEffectCreationHandler(MobileScoutAtEndOfTurnV1, MobileScoutDefinition),
            new ActiveEffectCreationHandler(MarkForDeathBountyV1, MarkForDeathBountyDefinition),
            new ActiveEffectCreationHandler(
                HuntingSeasonFirstBountyV1, HuntingSeasonFirstBountyDefinition),
            new ActiveEffectCreationHandler(
                HuntingSeasonSecondBountyV1, HuntingSeasonSecondBountyDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            DelayedJumpDefinition,
            MobileScoutDefinition,
            MarkForDeathBountyDefinition,
            HuntingSeasonFirstBountyDefinition,
            HuntingSeasonSecondBountyDefinition
        });
    }
}

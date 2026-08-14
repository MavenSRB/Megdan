using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8. The general typed modifier, restriction, permission, immunity, and
    /// effective-characteristic contracts, proved against synthetic definitions before any verified hero
    /// content exists.
    ///
    /// Rules authority is the second-printing rulebook: Counts As (p.17), Active-effect cancellation and
    /// simultaneous ordering (p.17), immunity and presence (p.18), ignoring obstacles and the obstacle
    /// rule (p.18), targeting versus presence (p.19), and token properties outliving Active effects
    /// (p.19).
    /// </summary>
    public sealed class RuleModificationTests
    {
        private static readonly HeroId RedHero = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly HeroId BlueHero = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
        private static readonly CardInstanceId SourceCard = new CardInstanceId("synthetic-modification-card");
        private static readonly ActionDefinitionId SourceAction = new ActionDefinitionId("synthetic-modification-action");

        // ---------------------------------------------------------------------------------------
        // Declarations
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Declaration_RequiresTheExactDataItsFamilyNeeds()
        {
            Assert.Throws<ArgumentException>(() =>
                RuleModification.ForValue(PrintedValueChannel.None, 1));
            Assert.Throws<ArgumentException>(() =>
                RuleModification.ForValue(PrintedValueChannel.Defense, 0));
            Assert.Throws<ArgumentException>(() => RuleModification.ForRestriction(RuleActivity.None));
            Assert.Throws<ArgumentException>(() => RuleModification.ForImmunity(RuleActivity.None));
            Assert.Throws<ArgumentException>(() => RuleModification.ForPermission(RulePermissionKind.None));
            Assert.Throws<ArgumentException>(() => RuleModification.ForCountsAs(EffectiveUnitKind.None));

            var restriction = RuleModification.ForRestriction(RuleActivity.FastTravel);
            Assert.That(restriction.Kind, Is.EqualTo(RuleModificationKind.Restriction));
            Assert.That(restriction.Activity, Is.EqualTo(RuleActivity.FastTravel));
            Assert.That(restriction.Permission, Is.EqualTo(RulePermissionKind.None));
            Assert.That(restriction.Channel, Is.EqualTo(PrintedValueChannel.None));
            Assert.That(restriction.EffectiveUnitKind, Is.EqualTo(EffectiveUnitKind.None));
        }

        [Test]
        public void DeclaredActivity_CoversItselfOrEverythingAndNothingElse()
        {
            var narrow = RuleModification.ForImmunity(RuleActivity.ObjectPush);
            var broad = RuleModification.ForImmunity(RuleActivity.All);

            Assert.That(narrow.Covers(RuleActivity.ObjectPush), Is.True);
            Assert.That(narrow.Covers(RuleActivity.Attack), Is.False);
            Assert.That(broad.Covers(RuleActivity.Attack), Is.True);
            Assert.That(broad.Covers(RuleActivity.ObjectPush), Is.True);
            Assert.Throws<ArgumentException>(() => narrow.Covers(RuleActivity.None));
        }

        [Test]
        public void ActionKinds_MapToActivitiesThroughAnExplicitSwitch()
        {
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Movement), Is.EqualTo(RuleActivity.Movement));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Attack), Is.EqualTo(RuleActivity.Attack));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Skill), Is.EqualTo(RuleActivity.Skill));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Defense), Is.EqualTo(RuleActivity.Defense));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Hold), Is.EqualTo(RuleActivity.Hold));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.FastTravel), Is.EqualTo(RuleActivity.FastTravel));
            Assert.That(RuleActivityQuery.ForAction(ActionKind.Clear), Is.EqualTo(RuleActivity.Clear));
            Assert.Throws<ArgumentOutOfRangeException>(() => RuleActivityQuery.ForAction(ActionKind.None));
        }

        // ---------------------------------------------------------------------------------------
        // Scope, precedence, and ordering
        // ---------------------------------------------------------------------------------------

        [Test]
        public void NarrowPermission_DoesNotDisableAnUnrelatedRestriction()
        {
            var hero = RuleObject.ForHero(RedHero);
            var modifications = Set(
                Applied(RuleModification.ForRestriction(RuleActivity.FastTravel), hero, "restriction", 0),
                Applied(
                    RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles),
                    hero,
                    "permission",
                    1));

            Assert.That(modifications.IsRestricted(hero, RuleActivity.FastTravel), Is.True);
            Assert.That(modifications.HasPermission(hero, RulePermissionKind.MoveThroughObstacles), Is.True);
            Assert.That(modifications.IsRestricted(hero, RuleActivity.Movement), Is.False);
            Assert.That(modifications.IsRestricted(hero, RuleActivity.Attack), Is.False);

            Assert.That(
                modifications.TryGetRestriction(hero, RuleActivity.FastTravel, out var deciding), Is.True);
            Assert.That(deciding.SourceId, Is.EqualTo("restriction"));
            Assert.That(deciding.AffectedObject, Is.EqualTo(hero));
            Assert.That(deciding.SourceKind, Is.EqualTo(RuleModificationSourceKind.ActiveEffect));
        }

        [Test]
        public void Restriction_ReachesOnlyItsOwnAffectedObject()
        {
            var red = RuleObject.ForHero(RedHero);
            var blue = RuleObject.ForHero(BlueHero);
            var modifications = Set(
                Applied(RuleModification.ForRestriction(RuleActivity.FastTravel), red, "red-restriction", 0));

            Assert.That(modifications.IsRestricted(red, RuleActivity.FastTravel), Is.True);
            Assert.That(modifications.IsRestricted(blue, RuleActivity.FastTravel), Is.False);
            Assert.That(modifications.For(blue), Is.Empty);
        }

        [Test]
        public void SimultaneousModifications_OrderByRevealedSideThenCreationThenIdentity()
        {
            var hero = RuleObject.ForHero(RedHero);
            var modifications = new RuleModificationSet(
                new[]
                {
                    Applied(Bonus(3), hero, "red-late", 5, TeamSide.Red),
                    Applied(Bonus(2), hero, "blue-early", 0, TeamSide.Blue),
                    Applied(Bonus(1), hero, "red-early", 1, TeamSide.Red),
                    Applied(Bonus(4), hero, "red-early-twin", 1, TeamSide.Red)
                },
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                modifications.Modifications.Select(value => value.SourceId),
                Is.EqualTo(new[] { "red-early", "red-early-twin", "red-late", "blue-early" }));

            // The same modifications with the coin showing the other side reorder, and nothing flips it.
            var flipped = new RuleModificationSet(
                modifications.Modifications, new TieBreakerState(TeamSide.Blue));
            Assert.That(
                flipped.Modifications.Select(value => value.SourceId),
                Is.EqualTo(new[] { "blue-early", "red-early", "red-early-twin", "red-late" }));
            Assert.That(modifications.TieBreaker.RevealedSide, Is.EqualTo(TeamSide.Red));
        }

        // ---------------------------------------------------------------------------------------
        // Immunity, presence, and effective type
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Immunity_CoversOnlyItsNamedActivityAndNeverTheTargetsOwnActions()
        {
            var red = RuleObject.ForHero(RedHero);
            var blue = RuleObject.ForHero(BlueHero);
            var narrow = Set(Applied(
                RuleModification.ForImmunity(RuleActivity.ObjectPush), red, "push-immunity", 0));
            var broad = Set(Applied(
                RuleModification.ForImmunity(RuleActivity.All), red, "total-immunity", 0));

            Assert.That(narrow.IsImmuneTo(red, RuleActivity.ObjectPush, blue), Is.True);
            Assert.That(narrow.IsImmuneTo(red, RuleActivity.Attack, blue), Is.False);
            Assert.That(broad.IsImmuneTo(red, RuleActivity.Attack, blue), Is.True);

            // p.18: "You are never immune to your own actions."
            Assert.That(broad.IsImmuneTo(red, RuleActivity.Attack, red), Is.False);
            Assert.That(narrow.IsImmuneTo(red, RuleActivity.ObjectPush, red), Is.False);

            // Asked in the abstract, with no actor performing anything, the declaration still holds.
            Assert.That(narrow.IsImmuneTo(red, RuleActivity.ObjectPush), Is.True);
            Assert.That(narrow.TryGetImmunity(red, RuleActivity.ObjectPush, blue, out var deciding), Is.True);
            Assert.That(deciding.SourceId, Is.EqualTo("push-immunity"));
        }

        [Test]
        public void CountsAs_ReplacesTheEffectiveTypeWithoutTouchingIntrinsicRepresentation()
        {
            var hero = RuleObject.ForHero(RedHero);
            var minion = RuleObject.ForMinion(new MinionId("blue-melee"));
            var modifications = Set(Applied(
                RuleModification.ForCountsAs(EffectiveUnitKind.Minion), hero, "counts-as", 0));

            // p.17 replaces the object's rules and qualities; it does not add to them.
            Assert.That(EffectiveUnitKindQuery.Is(hero, EffectiveUnitKind.Minion, modifications), Is.True);
            Assert.That(EffectiveUnitKindQuery.Is(hero, EffectiveUnitKind.Hero, modifications), Is.False);
            Assert.That(
                EffectiveUnitKindQuery.Of(hero, modifications),
                Is.EqualTo(new[] { EffectiveUnitKind.Minion }));
            Assert.That(EffectiveUnitKindQuery.Is(hero, EffectiveUnitKind.Hero, null), Is.True);

            // p.18's Battle-Zone correction is about miniatures, so the intrinsic query cannot see it.
            Assert.That(IntrinsicKindQuery.Of(hero), Is.EqualTo(RuleObjectKind.Hero));
            Assert.That(IntrinsicKindQuery.IntrinsicUnitKind(hero), Is.EqualTo(EffectiveUnitKind.Hero));
            Assert.That(IntrinsicKindQuery.IsMinionMiniature(hero), Is.False);
            Assert.That(IntrinsicKindQuery.IsMinionMiniature(minion), Is.True);
            Assert.That(hero.HeroId, Is.EqualTo(RedHero));
        }

        [Test]
        public void Presence_CountsImmuneUnitsAndReadsEffectiveTypeWhileTargetingRefusesTheImmuneOne()
        {
            var board = CreateBoard(
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(1, 0))
                },
                CreateMinion("blue-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));
            var participants = PlaceholderOneVsOneParticipants.Create();
            var blue = RuleObject.ForHero(BlueHero);
            var modifications = Set(
                Applied(RuleModification.ForImmunity(RuleActivity.Attack), blue, "attack-immunity", 0),
                Applied(RuleModification.ForCountsAs(EffectiveUnitKind.Minion), blue, "counts-as", 1));

            var targets = AttackTargetQuery.FindLegalEnemyTargets(
                RedHero, AttackRangeKind.NonRanged, 1, participants, board, modifications);
            var unmodifiedTargets = AttackTargetQuery.FindLegalEnemyTargets(
                RedHero, AttackRangeKind.NonRanged, 1, participants, board);
            var presentMinions = UnitPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 1, EffectiveUnitKind.Minion, board, modifications);

            // p.18: an immune unit cannot be targeted, but a presence check still counts it.
            Assert.That(unmodifiedTargets.Any(target => target.HeroId.Equals(BlueHero)), Is.True);
            Assert.That(targets.Any(target => target.HeroId.Equals(BlueHero)), Is.False);
            Assert.That(presentMinions, Contains.Item(blue));
            Assert.That(presentMinions, Contains.Item(RuleObject.ForMinion(new MinionId("blue-melee"))));
            Assert.That(
                UnitPresenceQuery.IsPresentWithin(
                    new HexCoordinate(0, 0), 1, EffectiveUnitKind.Minion, board, default, modifications),
                Is.True);

            // The same hero is no longer effectively a hero, so a hero-typed presence check misses them.
            Assert.That(
                UnitPresenceQuery.FindUnitsWithin(
                    new HexCoordinate(0, 0), 1, EffectiveUnitKind.Hero, board, modifications),
                Is.EqualTo(new[] { RuleObject.ForHero(RedHero) }));
        }

        [Test]
        public void Obstacles_BlockTransitUnlessThePermissionAllowsIt_AndAlwaysBlockAnEndpoint()
        {
            var occupied = new HexCoordinate(1, 0);
            var empty = new HexCoordinate(2, 0);
            var offBoard = new HexCoordinate(9, 0);
            var board = CreateBoard(
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, occupied)
                });
            var mover = RuleObject.ForHero(RedHero);
            var permitted = Set(Applied(
                RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles),
                mover,
                "move-through",
                0));

            Assert.That(ObstacleQuery.IsObstacle(board, occupied), Is.True);
            Assert.That(ObstacleQuery.IsObstacle(board, empty), Is.False);
            Assert.That(ObstacleQuery.IsObstacle(board, offBoard), Is.True);

            Assert.That(ObstacleQuery.BlocksTransit(board, occupied, mover, null), Is.True);
            Assert.That(ObstacleQuery.BlocksTransit(board, occupied, mover, permitted), Is.False);
            Assert.That(ObstacleQuery.BlocksTransit(board, offBoard, mover, permitted), Is.True);
            Assert.That(
                ObstacleQuery.BlocksTransit(board, occupied, RuleObject.ForHero(BlueHero), permitted),
                Is.True);

            // p.18: no unit may end its movement on an obstacle, permission or not.
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, occupied), Is.True);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, empty), Is.False);
        }

        // ---------------------------------------------------------------------------------------
        // Deriving the set from authoritative state
        // ---------------------------------------------------------------------------------------

        [Test]
        public void OnlyActiveEffectsSupplyRules_AndTokensSupplyThemWhilePlaced()
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var board = CreateBoard(new[] { new HeroOccupancy(RedHero, new HexCoordinate(0, 0)) });
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("dormant-next-turn"),
                SyntheticModificationCapabilities.MoveThroughPermissionDefinition,
                RedHero,
                TeamSide.Red,
                default,
                SourceCard,
                SourceAction,
                1,
                1);
            timing.Create(
                new ActiveEffectId("active-this-turn"),
                SyntheticModificationCapabilities.DefenseBonusDefinition,
                RedHero,
                TeamSide.Red,
                default,
                SourceCard,
                SourceAction,
                1,
                1);
            var hero = RuleObject.ForHero(RedHero);

            var beforeActivation = RuleModificationQuery.Build(
                timing, board, participants, new TieBreakerState(TeamSide.Red));
            Assert.That(
                beforeActivation.HasPermission(hero, RulePermissionKind.MoveThroughObstacles), Is.False);
            Assert.That(beforeActivation.ValueContributions(hero, PrintedValueChannel.Defense).Count, Is.EqualTo(1));

            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2, RedHero),
                new TieBreakerState(TeamSide.Red));
            var afterActivation = RuleModificationQuery.Build(
                timing, board, participants, new TieBreakerState(TeamSide.Red));
            Assert.That(
                afterActivation.HasPermission(hero, RulePermissionKind.MoveThroughObstacles), Is.True);

            // The token supplies its property for exactly as long as the instance is on the board.
            var withToken = CreateBoard(
                new[] { new HeroOccupancy(RedHero, new HexCoordinate(0, 0)) },
                null,
                CreateToken("hazard-1", new HexCoordinate(1, 0)));
            var tokenModifications = RuleModificationQuery.Build(
                timing, withToken, participants, new TieBreakerState(TeamSide.Red));
            var tokenContribution = tokenModifications
                .ValueContributions(hero, PrintedValueChannel.Defense)
                .Single(value => value.Kind == ValueContributionKind.ContinuousSourceModifier);

            Assert.That(tokenContribution.HasSourceContinuousSourceId, Is.True);
            Assert.That(tokenContribution.SourceContinuousSourceId, Does.StartWith("token:hazard-1:property:"));
            Assert.That(tokenContribution.Amount, Is.EqualTo(1));
            Assert.That(tokenContribution.HasSourceActiveEffectId, Is.False);
            Assert.That(
                RuleModificationQuery.Build(timing, board, participants, default)
                    .ValueContributions(hero, PrintedValueChannel.Defense)
                    .Any(value => value.Kind == ValueContributionKind.ContinuousSourceModifier),
                Is.False);
        }

        [Test]
        public void TeamAndGlobalScopes_ExpandToEveryHeroTheyName()
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("team-effect"),
                TeamScopedDefinition(ActiveEffectAffectedScopeKind.ControllerTeam, "team"),
                RedHero,
                TeamSide.Red,
                default,
                SourceCard,
                SourceAction,
                1,
                1);
            timing.Create(
                new ActiveEffectId("global-effect"),
                TeamScopedDefinition(ActiveEffectAffectedScopeKind.Global, "global"),
                RedHero,
                TeamSide.Red,
                default,
                SourceCard,
                SourceAction,
                1,
                1);

            var modifications = RuleModificationQuery.Build(
                timing, null, participants, new TieBreakerState(TeamSide.Red));

            Assert.That(
                modifications.For(RuleObject.ForHero(RedHero)).Select(value => value.SourceId),
                Is.EquivalentTo(new[] { "team-effect", "global-effect" }));
            Assert.That(
                modifications.For(RuleObject.ForHero(BlueHero)).Select(value => value.SourceId),
                Is.EqualTo(new[] { "global-effect" }));
        }

        // ---------------------------------------------------------------------------------------
        // Live composition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void DefenseTotal_ComposesPrintedMinionActiveAndContinuousContributions()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var participants = configuration.Participants;
            var board = CreateBoard(
                new[]
                {
                    new HeroOccupancy(RedHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(BlueHero, new HexCoordinate(1, 0))
                },
                CreateMinion("blue-friendly-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(2, 0)),
                CreateToken("hazard-1", new HexCoordinate(1, 1), BlueHero));
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("blue-defense-bonus"),
                SyntheticModificationCapabilities.DefenseBonusDefinition,
                BlueHero,
                TeamSide.Blue,
                default,
                SourceCard,
                SourceAction,
                1,
                1);
            var modifications = RuleModificationQuery.Build(
                timing, board, participants, new TieBreakerState(TeamSide.Red));
            var authority = new CardActionResolutionAuthority(board);
            var attackOffer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    RedHero,
                    ActionOfferWindow.NormalPlay)
                .Single(offer =>
                    offer.SourceCardInstanceId.Value == "red-bulwark-card" &&
                    offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            var started = authority.StartAttackResolution(
                new ActionResolutionId("modified-defense"),
                attackOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants,
                modifications);
            var opened = authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    started.PendingTargetChoice.ResolutionId,
                    started.PendingTargetChoice.ChoiceId,
                    BlueHero),
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants,
                modifications);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers[0];
            var printed = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer, configuration.CardDefinitions, PrintedValueChannel.Defense);
            var answered = authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants,
                modifications);

            Assert.That(answered.Accepted, Is.True);
            var contributions = answered.PendingResolution.DefenseValue.Contributions;
            Assert.That(
                contributions.Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    ValueContributionKind.PrintedIcon,
                    ValueContributionKind.MinionDefenseModifier,
                    ValueContributionKind.ActiveEffectModifier,
                    ValueContributionKind.ContinuousSourceModifier
                }));
            Assert.That(answered.PendingResolution.DefenseTotal, Is.EqualTo(printed.Total + 3));
            Assert.That(
                contributions.Single(value => value.Kind == ValueContributionKind.ActiveEffectModifier)
                    .SourceActiveEffectId.Value,
                Is.EqualTo("blue-defense-bonus"));
            Assert.That(
                contributions.Single(value => value.Kind == ValueContributionKind.ContinuousSourceModifier)
                    .SourceContinuousSourceId,
                Does.StartWith("token:hazard-1:property:"));
        }

        [Test]
        public void FastTravelOffer_DisappearsUnderItsRestrictionWhileOtherOffersRemain()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var red = configuration.RedHand.Instances[0];
            var blue = configuration.BlueHand.Instances[0];
            var board = CreateBoard(new[]
            {
                new HeroOccupancy(red.OwnerId, new HexCoordinate(0, 0)),
                new HeroOccupancy(blue.OwnerId, new HexCoordinate(1, 0))
            });
            var timing = new ActiveEffectLifecycle();
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForMatch(
                    new CardActionResolutionAuthority(board),
                    board,
                    timing,
                    new MarkerAttachmentRegister()));
            Assert.That(round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
            var selected = round.CurrentTurnAuthority.SelectedActor;
            var movementOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    selected.ActorId,
                    selected.CardInstanceId)
                .First(offer => ReplaceableMovement(offer, configuration.CardDefinitions));
            var hero = RuleObject.ForHero(selected.ActorId);
            var restricted = Set(Applied(
                RuleModification.ForRestriction(RuleActivity.FastTravel), hero, "no-fast-travel", 0));
            var unrelated = Set(Applied(
                RuleModification.ForRestriction(RuleActivity.Clear), hero, "no-clear", 0));

            var offered = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                movementOffer.ActionDefinitionId,
                movementOffer.SourceCardInstanceId,
                selected.ActorId,
                configuration.CardDefinitions,
                configuration.CardInstances);
            var refused = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                movementOffer.ActionDefinitionId,
                movementOffer.SourceCardInstanceId,
                selected.ActorId,
                configuration.CardDefinitions,
                configuration.CardInstances,
                restricted);
            var stillOffered = GeneralRuleActionOfferQuery.GetAvailableFastTravelOffer(
                movementOffer.ActionDefinitionId,
                movementOffer.SourceCardInstanceId,
                selected.ActorId,
                configuration.CardDefinitions,
                configuration.CardInstances,
                unrelated);

            Assert.That(offered.HasValue, Is.True);
            Assert.That(refused.HasValue, Is.False);
            Assert.That(stillOffered, Is.EqualTo(offered));

            // The live turn authority assembles the same set from its own timing state, so the
            // restriction reaches the offer through production wiring rather than only through a
            // set a fixture handed in.
            Assert.That(round.CurrentTurnAuthority.CurrentModifications().IsEmpty, Is.True);
            timing.Create(
                new ActiveEffectId("live-fast-travel-restriction"),
                SyntheticModificationCapabilities.FastTravelRestrictionDefinition,
                selected.ActorId,
                configuration.Participants.GetTeamForHero(selected.ActorId).Side,
                default,
                selected.CardInstanceId,
                movementOffer.ActionDefinitionId,
                1,
                1);
            Assert.That(
                round.CurrentTurnAuthority.CurrentModifications()
                    .IsRestricted(hero, RuleActivity.FastTravel),
                Is.True);
        }

        [Test]
        public void ModificationsSurviveSnapshotRestore_WithoutAnyRuleEnteringThePayload()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var red = configuration.RedHand.Instances[0];
            var blue = configuration.BlueHand.Instances[0];
            var board = CreateBoard(
                new[]
                {
                    new HeroOccupancy(red.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue.OwnerId, new HexCoordinate(1, 0))
                },
                null,
                CreateToken("hazard-restore", new HexCoordinate(1, 1), red.OwnerId));
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("restored-defense-bonus"),
                SyntheticModificationCapabilities.DefenseBonusDefinition,
                red.OwnerId,
                TeamSide.Red,
                default,
                red.Id,
                SourceAction,
                1,
                1);
            timing.Create(
                new ActiveEffectId("restored-counts-as"),
                SyntheticModificationCapabilities.CountsAsMinionDefinition,
                red.OwnerId,
                TeamSide.Red,
                default,
                red.Id,
                SourceAction,
                1,
                1);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForMatch(
                    new CardActionResolutionAuthority(board),
                    board,
                    timing,
                    new MarkerAttachmentRegister()));

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload, configuration.CardDefinitions, new[] { board.MapDefinition });
            var original = RuleModificationQuery.Build(
                timing, board, configuration.Participants, configuration.TieBreakerState);
            var reconstructed = RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                configuration.TieBreakerState);

            Assert.That(Facts(reconstructed), Is.EqualTo(Facts(original)));
            Assert.That(
                reconstructed.ValueContributions(
                    RuleObject.ForHero(red.OwnerId), PrintedValueChannel.Defense),
                Is.EqualTo(original.ValueContributions(
                    RuleObject.ForHero(red.OwnerId), PrintedValueChannel.Defense)));
            Assert.That(
                reconstructed.CountsAs(RuleObject.ForHero(red.OwnerId), EffectiveUnitKind.Minion), Is.True);

            // The payload names definitions and instances; it never names a rule. The proof is that
            // the reconstructed declarations are the trusted catalog's own objects, not equal copies
            // rebuilt from bytes.
            Assert.That(
                restored.Round.Timing.Effects
                    .Single(effect => effect.Id.Value == "restored-defense-bonus").Definition,
                Is.SameAs(SyntheticModificationCapabilities.DefenseBonusDefinition));
            Assert.That(
                restored.Board.PlacedTokens.Single().Definition.Properties[0].Modification,
                Is.SameAs(SyntheticComponentDefinitions.HazardDefinition.Properties[0].Modification));
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "An unchanged restored state must serialize deterministically.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static string[] Facts(RuleModificationSet modifications) =>
            modifications.Modifications
                .Select(value =>
                    $"{value.SourceKind}|{value.SourceId}|{value.AffectedObject}|{value.Modification}|" +
                    $"{value.ControllerHeroId.Value}|{value.ControllerTeamSide}|{value.OrderingSequence}")
                .ToArray();

        private static bool ReplaceableMovement(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var action = definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(candidate => candidate.Id.Equals(offer.ActionDefinitionId));
            return action.Kind == ActionKind.Movement && action.DefaultPolicy.CanBeReplaced;
        }

        private static RuleModification Bonus(int amount) =>
            RuleModification.ForValue(PrintedValueChannel.Defense, amount);

        private static AppliedRuleModification Applied(
            RuleModification modification,
            RuleObject affectedObject,
            string sourceId,
            long sequence,
            TeamSide side = TeamSide.Red) =>
            AppliedRuleModification.FromActiveEffect(
                modification,
                affectedObject,
                new ActiveEffectId(sourceId),
                affectedObject.Kind == RuleObjectKind.Hero ? affectedObject.HeroId : RedHero,
                side,
                sequence);

        private static RuleModificationSet Set(params AppliedRuleModification[] modifications) =>
            new RuleModificationSet(modifications, new TieBreakerState(TeamSide.Red));

        private static ActiveEffectDefinition TeamScopedDefinition(
            ActiveEffectAffectedScopeKind scope,
            string id) =>
            new ActiveEffectDefinition(
                $"synthetic-{id}-scope-effect",
                scope,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                false,
                "SyntheticScope",
                $"synthetic-{id}-scope",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForRestriction(RuleActivity.Clear) });

        private static AdjacentMoveAuthority CreateBoard(
            IEnumerable<HeroOccupancy> occupancies,
            QuickGameMinionSetupState minion = null,
            PlacedToken token = null)
        {
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                occupancies,
                Array.Empty<ObstacleOccupancy>(),
                minion == null
                    ? Array.Empty<QuickGameMinionSetupState>()
                    : new[] { minion });

            // A token reaches the board the same way production does: through the placement the board
            // itself commits, not through a setup constructor arm that does not exist.
            if (token != null)
            {
                board.CommitValidatedTokenPlacement(token);
            }

            return board;
        }

        private static PlacedToken CreateToken(
            string id,
            HexCoordinate coordinate,
            HeroId controller = default) =>
            new PlacedToken(
                new TokenInstanceId(id),
                SyntheticComponentDefinitions.HazardDefinition,
                controller.IsDefined ? controller : RedHero,
                controller.IsDefined ? controller : RedHero,
                default,
                coordinate);

        private static QuickGameMinionSetupState CreateMinion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            var mapId = new MapId("synthetic-coordinate-board");
            var teamId = new TeamId(side == TeamSide.Red
                ? PlaceholderOneVsOneParticipants.RedTeamIdValue
                : PlaceholderOneVsOneParticipants.BlueTeamIdValue);
            return new QuickGameMinionSetupState(
                new MinionId(id),
                teamId,
                side,
                type,
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(mapId, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);
        }

        private static IEnumerable<HexCoordinate> CreateRadiusThreeCoordinates()
        {
            for (var q = -3; q <= 3; q++)
            {
                var minimumR = Math.Max(-3, -q - 3);
                var maximumR = Math.Min(3, -q + 3);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.3: rulebook p.16's Ultimate - the Tier IV card a hero unlocks instead of an upgrade,
    /// the permanent ability it supplies, and the board edge that ability stops at.
    ///
    /// Organised by acceptance check, as milestones 7.1 and 7.2 organise theirs, because each of the
    /// three checks is about a relationship rather than a type: between the Ultimate and the ordinary
    /// effects and cards it must not be confused with, between its rule and where its hero is standing,
    /// and between the unlock and the upgrade state that permits it.
    /// </summary>
    public sealed class UltimateContinuousSourceTests
    {
        // ----- Check 1: distinct from ordinary cancelable Active effects and hand/deck cards -----

        [Test]
        public void TheUltimate_PrintsATierIVClassificationAnAbilityAndNothingElse()
        {
            var fixture = new UltimateFixture();

            foreach (var definition in new[] { fixture.RedUltimate, fixture.BlueUltimate })
            {
                var progression = definition.Progression;

                // p.16: "The Ultimate is a special Tier IV card."
                Assert.That(progression.Tier, Is.EqualTo(CardTier.IV), definition.Id.Value);
                Assert.That(progression.IsUltimate, Is.True, definition.Id.Value);
                Assert.That(progression.IsNonBasic, Is.False, definition.Id.Value);
                Assert.That(progression.IsBasic, Is.False, definition.Id.Value);

                // p.16 gives the Item icon to "every Tier II and Tier III Action card", and the level
                // that unlocks this one happens "instead of upgrading a card and gaining an item".
                Assert.That(progression.HasItemIcon, Is.False, definition.Id.Value);
                Assert.That(progression.NextTierCandidateIds, Is.Empty, definition.Id.Value);

                // The printed swatch is none of p.15's five hand colours, and no rule reads it.
                Assert.That(progression.Color, Is.EqualTo(CardColor.None), definition.Id.Value);

                // No value column and no action: the card is never played, only placed.
                Assert.That(definition.Initiative.IsDefined, Is.False, definition.Id.Value);
                Assert.That(definition.ActionDefinitions, Is.Empty, definition.Id.Value);

                // "It provides you with a powerful permanent ability."
                Assert.That(definition.UltimateRule, Is.Not.Null, definition.Id.Value);
                Assert.That(
                    definition.UltimateRule.Modification.Kind,
                    Is.EqualTo(RuleModificationKind.Permission),
                    definition.Id.Value);
                Assert.That(
                    UltimateRuleDefinition.ApplicabilityKind,
                    Is.EqualTo("WhileControllerHeroOnBoard"));
            }

            // Nothing else in the content set claims either the Tier or the classification, so no
            // ordinary card can be mistaken for one and no query has to exclude it by name.
            Assert.That(
                fixture.Definitions.Count(definition => definition.Progression.Tier == CardTier.IV),
                Is.EqualTo(2));
            Assert.That(
                fixture.Definitions.Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(2));
        }

        [Test]
        public void TheContentModel_RefusesEveryShapeThePageDoesNotPrint()
        {
            // Tier IV is the Ultimate's alone; an ordinary upgradeable card may not claim the numeral.
            Assert.Throws<ArgumentException>(() =>
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.IV, PrintedValueChannel.None));

            var rule = new UltimateRuleDefinition(
                "ability", "SyntheticPermission",
                RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles));

            // An Ultimate prints no Initiative, and an ordinary card still must.
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("ultimate-with-initiative"), "rev-1", "Ultimate",
                new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(), rule));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("ordinary-without-initiative"), "rev-1", "Ordinary",
                default, CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, Array.Empty<ActionDefinition>(), null));

            // The ability and the classification imply each other in both directions.
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("ultimate-without-ability"), "rev-1", "Ultimate",
                default, CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(), null));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("ordinary-with-ability"), "rev-1", "Ordinary",
                new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, Array.Empty<ActionDefinition>(), rule));

            // An ability with no rule would be a card whose whole printed content is missing.
            Assert.Throws<ArgumentNullException>(() =>
                new UltimateRuleDefinition("ability", "SyntheticPermission", null));

            // Nothing upgrades into an Ultimate: p.16 unlocks it instead.
            var ultimate = new CardDefinition(
                new CardDefinitionId("some-ultimate"), "rev-1", "Ultimate",
                default, CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(), rule);
            var tierThree = new CardDefinition(
                new CardDefinitionId("some-tier-three"), "rev-1", "Tier III",
                new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Attack));
            var tierTwo = new CardDefinition(
                new CardDefinitionId("some-tier-two"), "rev-1", "Tier II",
                new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Attack,
                    new CardDefinitionId("some-tier-three"),
                    new CardDefinitionId("some-ultimate")));
            var refusal = Assert.Throws<ArgumentException>(() =>
                CardProgressionValidator.Validate(new[] { ultimate, tierThree, tierTwo }));
            Assert.That(refusal.Message.Contains("unlocked instead of upgraded into"), Is.True);
        }

        [Test]
        public void AnUnlockedUltimate_IsInNeitherTheHandNorTheDeck()
        {
            var fixture = new UltimateFixture();
            fixture.UnlockFor(fixture.RedHeroId);
            var card = fixture.UltimateCardOf(fixture.RedHeroId);

            // p.16: "Place it Faceup to the left side of your hero dashboard, facing the opposing team."
            Assert.That(card.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(card.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(card.State.HasBeenPlayed, Is.False);

            // Not the hand a turn commits from: no action offer reaches it, and it prints none anyway.
            Assert.That(
                ActionOfferQuery.GetAvailableOffers(
                        fixture.Definitions,
                        fixture.Instances,
                        fixture.RedHeroId,
                        ActionOfferWindow.NormalPlay)
                    .Any(offer => offer.SourceCardInstanceId.Equals(card.Id)),
                Is.False);

            // Not the deck an upgrade searches, and not the discard pile retrieval reads.
            foreach (var location in new[]
                     {
                         CardLocation.Hand, CardLocation.Deck, CardLocation.DiscardPile,
                         CardLocation.SetAside, CardLocation.ItemSlot, CardLocation.TurnArea
                     })
            {
                Assert.That(
                    CardCharacteristicQuery.FindCandidates(
                            new CardCharacteristicCriteria(locations: new[] { location }),
                            fixture.Definitions,
                            fixture.Instances)
                        .Any(id => id.Equals(card.Id)),
                    Is.False,
                    location.ToString());
            }

            // p.8's Retrieve Cards returns played and discarded cards; this is neither.
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(card), Is.False);
            Assert.That(card.State.Location, Is.EqualTo(CardLocation.UltimateArea));
        }

        [Test]
        public void TheUltimatesSource_IsNotCancelledByAnythingThatCancelsAnActiveEffect()
        {
            var fixture = new UltimateFixture();
            fixture.UnlockFor(fixture.RedHeroId);
            var hero = RuleObject.ForHero(fixture.RedHeroId);

            // The comparison is exact: an ordinary Active effect supplying the same p.18 permission to
            // the same hero, created from the catalog the way a match creates one.
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId("ordinary-move-through"),
                GuardsDigital.Core.Effects.Synthetic.SyntheticModificationCapabilities
                    .MoveThroughPermissionDefinition,
                fixture.RedHeroId,
                TeamSide.Red,
                default,
                fixture.UltimateCardOf(fixture.RedHeroId).Id,
                new ActionDefinitionId("some-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2), Tie);

            var both = fixture.Modifications(timing);
            Assert.That(
                both.For(hero).Count(modification =>
                    modification.Modification.Permission == RulePermissionKind.MoveThroughObstacles),
                Is.EqualTo(2),
                "The comparison needs both sources supplying the same rule, or it proves nothing.");
            Assert.That(
                both.For(hero).Any(modification =>
                    modification.SourceKind == RuleModificationSourceKind.ContinuousSource),
                Is.True);

            // p.17's cancellations, applied one after another. Each of them ends an Active effect that
            // declares it; none of them is a question about where a card sits beside a dashboard.
            foreach (var checkpoint in new[]
                     {
                         TimingCheckpoint.SourceCardStateChanged,
                         TimingCheckpoint.ControllerHeroDefeated,
                         TimingCheckpoint.CardsRetrieved,
                         TimingCheckpoint.ExplicitCancellation,
                         TimingCheckpoint.RoundEnd
                     })
            {
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(
                        checkpoint,
                        1,
                        2,
                        affectedHeroId: fixture.RedHeroId,
                        sourceCardInstanceId: fixture.UltimateCardOf(fixture.RedHeroId).Id),
                    Tie);
            }

            Assert.That(timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));

            var afterwards = fixture.Modifications(timing);
            var remaining = afterwards.For(hero);
            Assert.That(remaining, Has.Count.EqualTo(1));
            Assert.That(
                remaining[0].SourceKind, Is.EqualTo(RuleModificationSourceKind.ContinuousSource));
            Assert.That(
                afterwards.HasPermission(hero, RulePermissionKind.MoveThroughObstacles), Is.True,
                "p.16's ability is permanent; nothing that cancels a card's Active effect touches it.");

            // It never entered the Active-effect collection at all, so the distinction is structural
            // rather than a matter of which conditions the two happen to declare.
            Assert.That(
                timing.Effects.Any(effect =>
                    effect.SourceCardInstanceId.Equals(fixture.UltimateCardOf(fixture.RedHeroId).Id) &&
                    effect.Phase != ActiveEffectPhase.Ended),
                Is.False);
            Assert.That(fixture.Unlocked().Ultimates, Has.Count.EqualTo(1));
        }

        // ----- Check 2: the board edge suppresses applicability without deleting the source -----

        [Test]
        public void TheAbility_AppliesOnlyWhileItsOwnHeroIsOnTheBoard()
        {
            var fixture = new UltimateFixture();
            fixture.UnlockFor(fixture.RedHeroId);
            fixture.UnlockFor(fixture.BlueHeroId);
            var red = RuleObject.ForHero(fixture.RedHeroId);
            var blue = RuleObject.ForHero(fixture.BlueHeroId);

            var withBoth = fixture.Modifications(fixture.CreateBoard(fixture.RedHeroId, fixture.BlueHeroId));
            Assert.That(withBoth.HasPermission(red, RulePermissionKind.MoveThroughObstacles), Is.True);
            Assert.That(withBoth.HasPermission(blue, RulePermissionKind.MoveThroughObstacles), Is.True);

            var withOneOffBoard = fixture.Modifications(fixture.CreateBoard(fixture.RedHeroId));
            Assert.That(
                withOneOffBoard.HasPermission(blue, RulePermissionKind.MoveThroughObstacles), Is.False,
                "p.16 applies the ability 'as long as your hero is on the board'.");
            Assert.That(
                withOneOffBoard.HasPermission(red, RulePermissionKind.MoveThroughObstacles), Is.True,
                "The suppression is one hero's board position, not a switch over the whole match.");

            // The unlocked source is untouched by the answer: the card is still beside the dashboard,
            // and the query that reads it still finds it. Nothing was deleted, so nothing is recreated
            // when the hero comes back - the same set is derived again from the same card.
            Assert.That(
                fixture.UltimateCardOf(fixture.BlueHeroId).State.Location,
                Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(fixture.Unlocked().IsUnlockedFor(fixture.BlueHeroId), Is.True);
            Assert.That(
                fixture.Modifications(fixture.CreateBoard(fixture.RedHeroId, fixture.BlueHeroId))
                    .HasPermission(blue, RulePermissionKind.MoveThroughObstacles),
                Is.True);
        }

        [Test]
        public void TheProductionConsumer_FollowsTheSameBoardAnswer()
        {
            var fixture = new UltimateFixture();
            fixture.UnlockFor(fixture.RedHeroId);
            var red = RuleObject.ForHero(fixture.RedHeroId);
            var onBoard = fixture.CreateBoard(fixture.RedHeroId, fixture.BlueHeroId);
            var occupied = onBoard.Occupancies
                .Single(occupancy => occupancy.HeroId.Equals(fixture.BlueHeroId))
                .Coordinate;

            // p.18's obstacle query is a real consumer of the permission, so the ability is observable
            // through the rule it supplies rather than only through the set that carries it.
            Assert.That(
                ObstacleQuery.BlocksTransit(onBoard, occupied, red, fixture.Modifications(onBoard)),
                Is.False);

            var offBoard = fixture.CreateBoard(fixture.BlueHeroId);
            Assert.That(
                ObstacleQuery.BlocksTransit(offBoard, occupied, red, fixture.Modifications(offBoard)),
                Is.True,
                "Off the board, the hero supplies no permission and p.18's obstacle blocks again.");
        }

        [Test]
        public void AnOffBoardHero_RegainsItsAbilityOnALegalRespawn()
        {
            // The same suppression and restoration, this time through the public command path rather
            // than by building two boards: the Blue hero starts off the board, and p.13's respawn puts
            // it back.
            var session = CreateSessionWithUnlockedUltimates(placeBlueHero: false);
            var blue = RuleObject.ForHero(new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue));
            var red = RuleObject.ForHero(new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue));

            var beforeRespawn = ModificationsOf(session);
            Assert.That(
                beforeRespawn.HasPermission(blue, RulePermissionKind.MoveThroughObstacles), Is.False);
            Assert.That(
                beforeRespawn.HasPermission(red, RulePermissionKind.MoveThroughObstacles), Is.True);
            Assert.That(
                session.Projection.Cards.Single(card =>
                    card.Id.Value == "blue-ultimate-card").State.Location,
                Is.EqualTo(CardLocation.UltimateArea),
                "Being off the board suppresses the ability without removing the unlocked card.");

            var redCard = session.Projection.Cards.First(card =>
                card.Id.Value.Contains("bombard") && card.State.Location == CardLocation.Hand);
            var blueCard = session.Projection.Cards.First(card =>
                card.Id.Value.Contains("feint") && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id));
            Accept(session, DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id));

            var pending = session.Projection.PendingHeroRespawnChoice;
            Assert.That(pending, Is.Not.Null);
            Accept(session, DirectorCommand.RespawnHero(pending.RespawnInvocations[0]));

            Assert.That(
                ModificationsOf(session).HasPermission(blue, RulePermissionKind.MoveThroughObstacles),
                Is.True,
                "A legal respawn restores applicability; nothing had to be unlocked a second time.");
        }

        // ----- Check 3: unlock eligibility is the Tier III upgrade state, named by no hero -----

        [Test]
        public void Unlock_RequiresEveryNonBasicCardOfThatHeroAtTierIII()
        {
            // Both heroes are asked the same question by the same code, which is what "without
            // hard-coding a hero name" means here: nothing in the query or the condition reads an
            // identity, and the reading names an owner the caller supplies.
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var fixture = new UltimateFixture();
                var heroId = side == TeamSide.Red ? fixture.RedHeroId : fixture.BlueHeroId;
                var reading = UltimateUnlockQuery.UpgradeCompletionReading(heroId);

                // The starting hand is p.6's: three Tier I cards and the Basic pair.
                Assert.That(
                    UltimateUnlockQuery.IsUpgradeComplete(reading, fixture.Definitions, fixture.Instances),
                    Is.False);
                Assert.That(
                    fixture.TryUnlock(heroId).RejectionReason,
                    Is.EqualTo(UltimateUnlockRejectionReason.UpgradeIncomplete));

                // p.15 upgrades each of them twice. With everything non-Basic at Tier III, the next
                // level is p.16's: "Once you upgrade all your cards to Tier III".
                fixture.UpgradeHandToTierThree(heroId);
                Assert.That(
                    UltimateUnlockQuery.IsUpgradeComplete(reading, fixture.Definitions, fixture.Instances),
                    Is.True);

                var unlocked = fixture.TryUnlock(heroId);
                Assert.That(unlocked.IsUnlocked, Is.True);
                Assert.That(unlocked.Ultimate.OwnerId, Is.EqualTo(heroId));
                Assert.That(
                    unlocked.Ultimate.Source.Kind, Is.EqualTo(ContinuousRuleSourceKind.Ultimate));
                Assert.That(
                    unlocked.Ultimate.Source.ApplicabilityKind,
                    Is.EqualTo(UltimateRuleDefinition.ApplicabilityKind));
                Assert.That(
                    unlocked.Ultimate.Source.SourceCardInstanceId,
                    Is.EqualTo(fixture.UltimateCardOf(heroId).Id));
            }
        }

        [Test]
        public void TheCondition_IsNotSatisfiedByAHandWithNothingLeftToUpgrade()
        {
            var fixture = new UltimateFixture();
            fixture.UpgradeHandToTierThree(fixture.RedHeroId);
            var reading = UltimateUnlockQuery.UpgradeCompletionReading(fixture.RedHeroId);
            Assert.That(
                UltimateUnlockQuery.IsUpgradeComplete(reading, fixture.Definitions, fixture.Instances),
                Is.True);

            // Emptying the hand of its non-Basic cards must not read as "everything is upgraded". The
            // condition is about better cards, not fewer.
            foreach (var card in fixture.HandOf(fixture.RedHeroId)
                         .Where(card => fixture.DefinitionOf(card).Progression.IsNonBasic)
                         .ToArray())
            {
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
            }

            Assert.That(
                UltimateUnlockQuery.IsUpgradeComplete(reading, fixture.Definitions, fixture.Instances),
                Is.False);
            Assert.That(
                fixture.TryUnlock(fixture.RedHeroId).RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.UpgradeIncomplete));
        }

        [Test]
        public void Unlock_RefusesEveryCardAndStateThePageDoesNotUnlock()
        {
            var fixture = new UltimateFixture();
            fixture.UpgradeHandToTierThree(fixture.RedHeroId);
            fixture.UpgradeHandToTierThree(fixture.BlueHeroId);
            var card = fixture.UltimateCardOf(fixture.RedHeroId);

            // The reading covers a different hero than the card belongs to.
            Assert.That(
                UltimateUnlockQuery.TryUnlock(
                        card,
                        UltimateUnlockQuery.UpgradeCompletionReading(fixture.BlueHeroId),
                        fixture.Definitions,
                        fixture.Instances)
                    .RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.NotOwnedByHero));

            // An ordinary card is not "the Ultimate card".
            var ordinary = fixture.HandOf(fixture.RedHeroId).First();
            Assert.That(
                UltimateUnlockQuery.TryUnlock(
                        ordinary,
                        UltimateUnlockQuery.UpgradeCompletionReading(fixture.RedHeroId),
                        fixture.Definitions,
                        fixture.Instances)
                    .RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.NotAnUltimate));

            // Content this set does not supply cannot say whether it is an Ultimate at all.
            Assert.That(
                UltimateUnlockQuery.TryUnlock(
                        card,
                        UltimateUnlockQuery.UpgradeCompletionReading(fixture.RedHeroId),
                        fixture.Definitions.Where(definition => !definition.Progression.IsUltimate),
                        fixture.Instances)
                    .RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.UnresolvedDefinition));

            // p.6 leaves the card in the deck and p.16 takes it from there.
            Assert.That(fixture.TryUnlock(fixture.RedHeroId).IsUnlocked, Is.True);
            Assert.That(
                fixture.TryUnlock(fixture.RedHeroId).RejectionReason,
                Is.EqualTo(UltimateUnlockRejectionReason.AlreadyUnlocked));

            // A refusal moves nothing: the Blue card is still in the deck after every one of them.
            Assert.That(
                fixture.UltimateCardOf(fixture.BlueHeroId).State.Location,
                Is.EqualTo(CardLocation.Deck));
        }

        // ----- Phase 7's durability rule -----

        [Test]
        public void AnUnlockedUltimate_SurvivesSnapshotRestoreReplayUndoAndARefusedCommand()
        {
            var session = CreateSessionWithUnlockedUltimates();
            var beforeAnyCommand = session.CaptureSnapshot();

            // A refused command changes nothing, the unlocked card included.
            Assert.That(session.Execute(DirectorCommand.StopMovement()).Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));

            var red = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue &&
                card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.BlueHeroIdValue &&
                card.State.Location == CardLocation.Hand);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            var afterFirstCommit = session.CaptureSnapshot();
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            var afterBothCommits = session.CaptureSnapshot();

            // Retained-prefix Undo truncates back and replaying the same command reconstructs forward.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterFirstCommit));
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterBothCommits));

            // Restart replays from the start, so the unlocked state is reconstructed rather than kept.
            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));

            // Restore-and-continue through the same public command path reproduces the same payload.
            var restoredSession = CreateSessionWithUnlockedUltimates();
            Assert.That(restoredSession.RestoreSnapshot(afterFirstCommit).Accepted, Is.True);
            Assert.That(restoredSession.CaptureSnapshot(), Is.EqualTo(afterFirstCommit));
            Assert.That(
                restoredSession.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted,
                Is.True);
            Assert.That(restoredSession.CaptureSnapshot(), Is.EqualTo(afterBothCommits));

            // The unlock reads back as the state itself rather than as a default, and the rule it
            // supplies is derived again from the restored cards without the payload carrying it.
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(afterBothCommits, out var envelope, out _),
                Is.True);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            var unlocked = UltimateUnlockQuery.Derive(session.Projection.Definitions, restored.Cards);
            Assert.That(unlocked.Ultimates, Has.Count.EqualTo(2));
            Assert.That(
                unlocked.Ultimates.Select(ultimate => ultimate.OwnerId.Value),
                Is.EqualTo(restored.Participants.Heroes
                    .Select(hero => hero.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray()));

            var modifications = RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState,
                unlocked, RuleObject.None);
            foreach (var hero in restored.Participants.Heroes)
            {
                Assert.That(
                    modifications.HasPermission(
                        RuleObject.ForHero(hero.Id), RulePermissionKind.MoveThroughObstacles),
                    Is.True,
                    hero.Id.Value);
            }
        }

        [Test]
        public void Restore_RefusesAnUltimateAreaRulebookPageSixteenCouldNotHaveProduced()
        {
            // A card beside a dashboard that is not an Ultimate.
            var notAnUltimate = CreateSession(instances => instances
                .Select(card => card.State.Location == CardLocation.Hand &&
                        card.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue
                    ? BesideTheDashboard(card)
                    : card)
                .ToArray());
            AssertRestoreRefuses(
                notAnUltimate, SnapshotLoadRejectionReason.UnlockedUltimateMismatch);

            // Two Ultimates for one hero. p.16 unlocks one.
            var twoForOneHero = CreateSession(instances =>
            {
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture();
                var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
                var ultimate = configuration.RedHand.Definitions
                    .Single(definition => definition.Progression.IsUltimate);
                return instances.Concat(new[]
                {
                    UltimateInstance("red-ultimate-one", ultimate, redHeroId, CardLocation.UltimateArea),
                    UltimateInstance("red-ultimate-two", ultimate, redHeroId, CardLocation.UltimateArea)
                }).ToArray();
            });
            AssertRestoreRefuses(
                twoForOneHero, SnapshotLoadRejectionReason.UnlockedUltimateMismatch);
        }

        // ----- Fixture -----

        private static TieBreakerState Tie => new TieBreakerState(TeamSide.Red);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// The modifications a live session's own state supplies, derived the way the match derives
        /// them: from the restored authority rather than from anything the projection carries.
        /// </summary>
        private static RuleModificationSet ModificationsOf(MatchSession session)
        {
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(
                    session.CaptureSnapshot(), out var envelope, out _),
                Is.True);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            return RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState,
                UltimateUnlockQuery.Derive(session.Projection.Definitions, restored.Cards), RuleObject.None);
        }

        private static void AssertRestoreRefuses(
            MatchSession session,
            SnapshotLoadRejectionReason reason)
        {
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(
                    session.CaptureSnapshot(), out var envelope, out _),
                Is.True);
            var refusal = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    envelope.AuthoritativePayload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    session.Projection.Setup.Minions));
            Assert.That(refusal.Reason, Is.EqualTo(reason));
        }

        private static CardInstance BesideTheDashboard(CardInstance card) =>
            new CardInstance(
                card.Id,
                card.DefinitionReference,
                card.OwnerId,
                new CardStateFacts(
                    CardLocation.UltimateArea, false, CardResolutionStatus.None,
                    CardFaceOrientation.FaceUp));

        private static CardInstance UltimateInstance(
            string id,
            CardDefinition definition,
            HeroId ownerId,
            CardLocation location) =>
            new CardInstance(
                new CardInstanceId(id),
                definition.Reference,
                ownerId,
                new CardStateFacts(
                    location, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp));

        /// <summary>
        /// Milestone 7.5 deals each hero's Ultimate into their deck, so this moves the dealt card to
        /// where p.16 puts an unlocked one rather than adding a second copy beside it.
        /// </summary>
        private static MatchSession CreateSessionWithUnlockedUltimates(bool placeBlueHero = true) =>
            CreateSession(instances =>
            {
                var ultimateIds = UltimateDefinitionIds();
                return instances
                    .Select(card => ultimateIds.Contains(card.DefinitionReference.DefinitionId)
                        ? new CardInstance(
                            card.Id,
                            card.DefinitionReference,
                            card.OwnerId,
                            new CardStateFacts(
                                CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                CardFaceOrientation.FaceUp))
                        : card)
                    .ToArray();
            },
            placeBlueHero);

        private static HashSet<CardDefinitionId> UltimateDefinitionIds() =>
            new HashSet<CardDefinitionId>(PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture()
                .CardDefinitions
                .Where(definition => definition.Progression.IsUltimate)
                .Select(definition => definition.Id));

        /// <summary>
        /// A configured session whose cards this fixture shapes before the match starts.
        ///
        /// Nothing in this build unlocks an Ultimate during a live match - p.16 attaches the unlock to a
        /// level purchase, which is milestone 7.5's - so the durability evidence starts from a match
        /// that already has one, exactly as milestone 7.1 proved a non-default Level and milestone 7.2 a
        /// placed item through their own restore contracts rather than through a purchase this build
        /// cannot reach.
        /// </summary>
        private static MatchSession CreateSession(
            Func<IReadOnlyList<CardInstance>, IReadOnlyList<CardInstance>> shapeCards,
            bool placeBlueHero = true)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                shapeCards(configuration.CardInstances),
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes
                    .Where(hero => placeBlueHero || hero.TeamSide == TeamSide.Red)
                    .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// The configured content plus one deck-resident Ultimate card per hero - p.6's arrangement,
        /// where setup takes five cards into hand and returns "the remaining Tier II, Tier III, and
        /// Tier IV cards to your deck".
        /// </summary>
        private sealed class UltimateFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private readonly List<CardInstance> instances;

            public UltimateFixture()
            {
                configuration = PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture();
                RedHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
                BlueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
                RedUltimate = configuration.RedHand.Definitions
                    .Single(definition => definition.Progression.IsUltimate);
                BlueUltimate = configuration.BlueHand.Definitions
                    .Single(definition => definition.Progression.IsUltimate);
                // Milestone 7.5 deals the Ultimate into each hero's deck, which is where p.6 leaves it
                // and where p.16 takes it from, so this fixture no longer builds one.
                instances = configuration.CardInstances.ToList();
            }

            public HeroId RedHeroId { get; }

            public HeroId BlueHeroId { get; }

            public CardDefinition RedUltimate { get; }

            public CardDefinition BlueUltimate { get; }

            public IReadOnlyList<CardDefinition> Definitions => configuration.CardDefinitions;

            public IReadOnlyList<CardInstance> Instances => instances;

            public CardDefinition DefinitionOf(CardInstance card) => Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference));

            public IReadOnlyList<CardInstance> HandOf(HeroId heroId) => instances
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .ToArray();

            public CardInstance UltimateCardOf(HeroId heroId) => instances
                .Single(card => card.OwnerId.Equals(heroId) &&
                    DefinitionOf(card).Progression.IsUltimate);

            public UnlockedUltimateSet Unlocked() =>
                UltimateUnlockQuery.Derive(Definitions, instances);

            public UltimateUnlockResult TryUnlock(HeroId heroId) => UltimateUnlockQuery.TryUnlock(
                UltimateCardOf(heroId),
                UltimateUnlockQuery.UpgradeCompletionReading(heroId),
                Definitions,
                instances);

            /// <summary>
            /// Unlocks without the p.16 condition, for the cases that are about what an unlocked
            /// Ultimate does rather than about when one may be unlocked.
            /// </summary>
            public void UnlockFor(HeroId heroId)
            {
                UpgradeHandToTierThree(heroId);
                Assert.That(TryUnlock(heroId).IsUnlocked, Is.True);
            }

            /// <summary>
            /// p.15's Upgrade a Card step run to its end for one hero: every non-Basic card in hand is
            /// replaced by the Tier III card its own upgrade tree leads to. The relationship comes from
            /// the content rather than from an id: each card names its next-Tier candidates, and the
            /// walk follows them until there are none left to follow.
            /// </summary>
            public void UpgradeHandToTierThree(HeroId heroId)
            {
                foreach (var card in HandOf(heroId))
                {
                    var definition = DefinitionOf(card);
                    if (!definition.Progression.IsNonBasic)
                    {
                        continue;
                    }

                    while (definition.Progression.Tier != CardTier.III)
                    {
                        definition = CardCharacteristicQuery
                            .FindUpgradeCandidates(definition.Id, Definitions)
                            .First();
                    }

                    instances[instances.IndexOf(card)] = new CardInstance(
                        card.Id, definition.Reference, card.OwnerId, card.State);
                }
            }

            /// <summary>
            /// The official map with exactly the named heroes standing on it, at their own spawn
            /// points. A hero left out is a hero off the board, which is the only thing p.16's
            /// applicability question asks about.
            /// </summary>
            public AdjacentMoveAuthority CreateBoard(params HeroId[] onBoard)
            {
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);
                return new AdjacentMoveAuthority(
                    map,
                    setup.Heroes
                        .Where(hero => onBoard.Any(heroId => heroId.Equals(hero.HeroId)))
                        .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    Array.Empty<ObstacleOccupancy>(),
                    setup.Minions);
            }

            public RuleModificationSet Modifications(AdjacentMoveAuthority board) =>
                RuleModificationQuery.Build(
                    null, board, configuration.Participants, Tie, Unlocked(), RuleObject.None);

            public RuleModificationSet Modifications(ActiveEffectLifecycle timing) =>
                RuleModificationQuery.Build(
                    timing,
                    CreateBoard(RedHeroId, BlueHeroId),
                    configuration.Participants,
                    Tie,
                    Unlocked(), RuleObject.None);
        }
    }
}

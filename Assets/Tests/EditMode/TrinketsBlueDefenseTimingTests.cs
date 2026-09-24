using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsBlueDefenseTimingTests
    {
        private static readonly HeroId Trinkets =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Attacker =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly HeroId Defender =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [Test]
        public void SolePrimaryDefenseCard_IsDiscardedBeforeDefenseOffer()
            => RunDefenseTiming(false);

        [Test]
        public void GridWithEmptyDefenderHand_DefeatsBeforeDefenseOffer()
            => RunDefenseTiming(true);

        private static void RunDefenseTiming(bool grid)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var spaces = Enumerable.Range(-4, 9)
                .SelectMany(q => Enumerable.Range(-4, 9)
                    .Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("trinkets-blue-defense"), 1, spaces),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Attacker, new HexCoordinate(1, 0)),
                    new HeroOccupancy(Defender, new HexCoordinate(0, 2))
                }, Array.Empty<ObstacleOccupancy>());
            Assert.That(board.TryPlaceOrRepositionObstacle(
                OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets),
                new HexCoordinate(0, 1)), Is.True);
            var source = TrinketsCardContent.DisruptorDefinitions[grid ? 2 : 0];
            var sourceInstance = CardInstance.CreateInHand(
                new CardInstanceId("trinkets-jolt"), source.Reference, Trinkets);
            var attackCard = AttackCard();
            var attackInstance = CardInstance.CreateInHand(
                new CardInstanceId("blue-attack"), attackCard.Reference, Attacker);
            var playedRed = HoldCard();
            var redInstance = CardInstance.CreateInHand(
                new CardInstanceId("red-played"), playedRed.Reference, Defender);
            var defenseCard = PrimaryDefenseCard();
            var defenseInstance = CardInstance.CreateInHand(
                new CardInstanceId("red-only-defense"), defenseCard.Reference, Defender);
            var definitions = new[] { source, attackCard, playedRed, defenseCard };
            var instances = grid
                ? new[] { sourceInstance, attackInstance, redInstance }
                : new[] { sourceInstance, attackInstance, redInstance, defenseInstance };
            var timing = new ActiveEffectLifecycle();
            timing.Create(new ActiveEffectId("jolt-active"),
                grid ? TrinketsActiveEffectContent.Grid : TrinketsActiveEffectContent.Jolt,
                Trinkets, TeamSide.Blue, default, sourceInstance.Id,
                TrinketsCardContent.PrimarySkill, 1, 1);
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    TrinketsActiveEffectContent.Handlers()),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(new[]
                {
                    TrinketsEffectContent.DisruptorTrigger(
                        source.Id, TrinketsCardContent.ContentRevision, grid)
                }));
            var actions = new CardActionResolutionAuthority(
                catalogs, board, new MarkerAttachmentRegister(),
                new HeroProgressionState(participants), new TeamLifeState(participants, 8));
            var round = new RoundResolutionAuthority(
                definitions, instances, participants,
                new TieBreakerState(TeamSide.Blue), 1,
                new RoundResolutionComposition(
                    actions, board, timing, new MarkerAttachmentRegister(),
                    null, null, null, new TeamLifeState(participants, 8), null, null));
            Assert.That(round.TryCommitCard(Trinkets, sourceInstance.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Attacker, attackInstance.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Defender, redInstance.Id).Accepted, Is.True);
            var turn = round.CurrentTurnAuthority;
            Assert.That(turn.SelectedActor.CardInstanceId, Is.EqualTo(attackInstance.Id));
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                definitions, instances, Attacker, attackInstance.Id)
                .Single(value => value.ActionDefinitionId.Value == "primary-attack");
            Assert.That(turn.TryStartAttack(
                new ActionResolutionId("blue-attack-resolution"), offer).Accepted, Is.True);
            var target = turn.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null);
            Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                target.ResolutionId, target.ChoiceId, Defender)).Accepted, Is.True);
            if (grid)
            {
                Assert.That(board.TryGetHeroCoordinate(Defender, out _), Is.False);
                Assert.That(turn.PendingDefenseChoice, Is.Null);
                Assert.That(timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
                return;
            }
            Assert.That(turn.PendingDefenseChoice, Is.Null,
                "p.14 has not offered the sole Defense card before disruption");
            var discard = turn.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null);
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, payload, definitions, new[] { board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());
            Assert.That(restored.Round.CurrentTurnAuthority.PendingEffectChoice, Is.Not.Null);
            Assert.That(restored.Round.CurrentTurnAuthority.PendingDefenseChoice, Is.Null);
            var restoredActions = CardActionResolutionAuthority.Restore(
                catalogs, board, actions.CaptureRestoreState(), null, null, null,
                definitions, instances, participants, new MarkerAttachmentRegister(),
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty);
            Assert.That(restoredActions.PendingDefenseChoice, Is.Null);
            Assert.That(restoredActions.PendingPreActionResolution?.Frame, Is.Not.Null);
            Assert.That(discard.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.CardInstance &&
                candidate.Value.CardInstanceId.Equals(defenseInstance.Id)), Is.True);
            var chosen = discard.SelectInvocations.Single(command =>
                command.Candidate.Value.CardInstanceId.Equals(defenseInstance.Id));
            Assert.That(restored.Round.TryAnswerEffectChoice(chosen).Accepted, Is.True);
            var restoredAfter = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            if (restoredAfter != null)
                Assert.That(restored.Round.TryAnswerEffectChoice(
                    restoredAfter.ConfirmInvocation).Accepted, Is.True);
            Assert.That(restored.Round.CurrentTurnAuthority.PendingDefenseChoice, Is.Not.Null);
            Assert.That(restored.Round.CurrentTurnAuthority.PendingDefenseChoice.LegalDefenseOffers,
                Is.Empty);
            Assert.That(restored.Round.Timing.Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(turn.TryAnswerEffectChoice(chosen).Accepted, Is.True);
            var after = turn.PendingEffectChoice;
            if (after != null)
                Assert.That(turn.TryAnswerEffectChoice(after.ConfirmInvocation).Accepted, Is.True);
            Assert.That(defenseInstance.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(turn.PendingDefenseChoice, Is.Not.Null);
            Assert.That(turn.PendingDefenseChoice.LegalDefenseOffers, Is.Empty);
        }

        private static CardDefinition AttackCard() => new CardDefinition(
            new CardDefinitionId("blue-test-attack"), "rev-1", "Attack",
            new InitiativeValue(20), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("primary-attack"),
                    ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged, default)
            });

        private static CardDefinition HoldCard() => new CardDefinition(
            new CardDefinitionId("red-test-hold"), "rev-1", "Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("secondary-movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static CardDefinition PrimaryDefenseCard() => new CardDefinition(
            new CardDefinitionId("red-test-primary-defense"), "rev-1", "Defense",
            new InitiativeValue(15), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("primary-defense"),
                    ActionKind.Defense, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 8) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });
    }
}

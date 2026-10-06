using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherRevealJourneyTests
    {
        [Test]
        public void LaughterCardOrderSwap_ExchangesOnlyResolvedSlots_AndRestores()
        {
            var setup = MatchConfiguration.CreateStagedNebKherForInternalTest(TeamSide.Red);
            var nebkher = setup.BlueHand.Instances[0].OwnerId;
            var wasp = setup.RedHand.Instances[0].OwnerId;
            var red = setup.RedHand.Instances.Take(2).ToArray();
            var laugh = setup.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    NebKherCardContent.DiabolicalLaughter));
            var blue = setup.BlueHand.Instances.Where(card => !card.Id.Equals(laugh.Id))
                .Take(2).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(new MapId("nebkher-card-order"), 1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var board = new AdjacentMoveAuthority(map,
                new[] { new HeroOccupancy(nebkher, new HexCoordinate(0, 0)),
                    new HeroOccupancy(wasp, new HexCoordinate(1, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(setup.CardDefinitions,
                setup.CardInstances, setup.Participants, setup.TieBreakerState, 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalogs, board), board,
                    new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                    null, null, null, null, null, null));
            for (var index = 0; index < 2; index++)
            {
                Assert.That(round.TryCommitCard(wasp, red[index].Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(nebkher, blue[index].Id).Accepted, Is.True);
                for (var guard = 0;
                    round.Phase == RoundResolutionPhase.ResolvingTurn && guard < 8; guard++)
                {
                    var turn = round.CurrentTurnAuthority;
                    var result = turn.PendingTurnRelease != null
                        ? round.TryReleaseTurn()
                        : round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                    Assert.That(result.Accepted, Is.True);
                }
            }
            Assert.That(round.TurnIndex, Is.EqualTo(3));
            Assert.That(round.TryCommitCard(wasp,
                setup.RedHand.Instances[2].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(nebkher, laugh.Id).Accepted, Is.True);
            Assert.That(red.All(CardLifecycleAuthority.IsResolvedCardInTurnArea), Is.True,
                string.Join(" / ", red.Select(card => card.Id + ":" + card.State)));
            var before = red.Select(card => card.State).ToArray();
            var handler = catalogs.Capabilities.Load(
                SyntheticResolvedCardOrderSwapCapabilities.SwapResolvedPairV1);
            var context = new EffectCapabilityContext(
                new EffectNodeId("laughter-swap-pair"), EffectFailurePolicy.Mandatory,
                nebkher, nebkher, laugh.Id, laugh.DefinitionReference,
                NebKherCardContent.PrimarySkill,
                new Dictionary<string, EffectValue>
                {
                    [SyntheticResolvedCardOrderSwapCapabilities.OwnerParameterName] =
                        EffectValue.Hero(wasp)
                },
                1, 3, setup.Participants, round.Timing, RuleModificationSet.Empty,
                EffectCardState.Of(setup.CardDefinitions, setup.CardInstances),
                ActionKind.Skill,
                rosterState: EffectRosterState.Of(round.TurnSlots,
                    new RoundHeroDefeatHistory(setup.Participants),
                    new HeroCoinLedger(setup.Participants),
                    resolvedCardOrderSwap: round));
            Assert.That(handler.Execute(context).Choice.Candidates,
                Is.EquivalentTo(red.Select(card => EffectValue.Card(card.Id))));
            Assert.That(handler.Resume(context, red.Select(card =>
                EffectValue.Card(card.Id)).ToArray()).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(round.TurnSlots[0].Assignments.Single(assignment =>
                assignment.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(red[1].Id));
            Assert.That(round.TurnSlots[1].Assignments.Single(assignment =>
                assignment.HeroId.Equals(wasp)).CardInstanceId, Is.EqualTo(red[0].Id));
            Assert.That(red.Select(card => card.State), Is.EqualTo(before));
            var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot,
                setup.CardDefinitions, new[] { map });
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(snapshot));
        }

        [Test]
        public void PrintedLaugh_InterruptsImmediatelyWithUnlockedUltimateDiscard()
        {
            var setup = MatchConfiguration.CreateStagedNebKherForInternalTest(TeamSide.Red);
            var nebkher = setup.BlueHand.Instances[0].OwnerId;
            var wasp = setup.RedHand.Instances[0].OwnerId;
            var laugh = setup.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    NebKherCardContent.DiabolicalLaughter));
            var ultimate = setup.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(
                    NebKherCardContent.WhatTheHellAreYou));
            var shock = setup.RedHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Shock));
            var gold = setup.RedHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.MagneticDagger));
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) },
                new[] { new HeroOccupancy(nebkher, new HexCoordinate(0, 0)),
                    new HeroOccupancy(wasp, new HexCoordinate(1, 0)) });
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(setup.CardDefinitions,
                setup.CardInstances, setup.Participants, setup.TieBreakerState, 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalogs, board), board,
                    new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                    null, null, null, null, null, null));
            Assert.That(round.TryCommitCard(wasp, shock.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(nebkher, laugh.Id).Accepted, Is.True);
            if (round.CurrentTurnAuthority.SelectedActor.ActorId.Equals(wasp))
            {
                Assert.That(round.TryPerformHold(shock.Id).Accepted, Is.True);
                if (round.CurrentTurnAuthority.PendingTurnRelease != null)
                    Assert.That(round.TryReleaseTurn().Accepted, Is.True);
            }
            Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(nebkher));
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                setup.CardDefinitions, setup.CardInstances, nebkher, laugh.Id)
                .Single(candidate => candidate.ActionDefinitionId.Equals(
                    NebKherCardContent.PrimarySkill));
            Assert.That(round.TryStartSkill(new ActionResolutionId("nebkher-laugh"), offer)
                .Accepted, Is.True);
            var choice = round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(choice.ConfirmInvocation, Is.Not.Null);
            Assert.That(round.TryAnswerEffectChoice(choice.ConfirmInvocation).Accepted, Is.True);
            choice = round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null,
                "The Ultimate must interrupt before the first of the three laughter choices.");
            Assert.That(choice.ChooserId, Is.EqualTo(wasp));
            Assert.That(choice.Candidates.Select(candidate => candidate.Value),
                Has.Member(EffectValue.Card(gold.Id)));
            Assert.That(round.TryAnswerEffectChoice(choice.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(EffectValue.Card(gold.Id))))
                .Accepted, Is.True);
            Assert.That(gold.State.Location, Is.EqualTo(CardLocation.DiscardPile));
        }

        [Test]
        public void NamedColorDiscard_PausesAtNextReveal_AndRestoresBeforeInitiative()
        {
            var setup = MatchConfiguration.CreateStagedNebKherForInternalTest(TeamSide.Red);
            var nebkher = setup.BlueHand.Instances[0].OwnerId;
            var wasp = setup.RedHand.Instances[0].OwnerId;
            CardInstance Blue(CardDefinitionId id) => setup.BlueHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(id));
            CardInstance Red(CardDefinitionId id) => setup.RedHand.Instances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(id));
            var source = Blue(NebKherCardContent.ImbueDoubt);
            var gold = Red(WaspCardContent.MagneticDagger);
            var map = BoardMapDefinition.CreateSynthetic(new MapId("nebkher-reveal"), 1,
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0) });
            var board = new AdjacentMoveAuthority(map,
                new[] { new HeroOccupancy(nebkher, new HexCoordinate(0, 0)),
                    new HeroOccupancy(wasp, new HexCoordinate(1, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var timing = new ActiveEffectLifecycle();
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(setup.CardDefinitions,
                setup.CardInstances, setup.Participants, setup.TieBreakerState, 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalogs, board), board, timing,
                    new MarkerAttachmentRegister(), null, null, null, null, null, null));

            Assert.That(round.TryCommitCard(wasp, Red(WaspCardContent.Shock).Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(nebkher, source.Id).Accepted,
                Is.True);
            var cast = ActionOfferQuery.GetAvailableTurnActionOffers(
                setup.CardDefinitions, setup.CardInstances, nebkher, source.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    NebKherCardContent.PrimarySkill));
            Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(nebkher));
            Assert.That(round.TryStartSkill(new ActionResolutionId("imbue-gold"), cast).Accepted,
                Is.True);
            var color = round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(color, Is.Not.Null);
            Assert.That(color.SelectInvocations, Has.Count.EqualTo(6));
            Assert.That(round.TryAnswerEffectChoice(color.SelectInvocations[0]).Accepted,
                Is.True);
            for (var guard = 0;
                round.Phase == RoundResolutionPhase.ResolvingTurn && guard < 8; guard++)
            {
                var turn = round.CurrentTurnAuthority;
                var step = turn.PendingTurnRelease != null
                    ? round.TryReleaseTurn()
                    : round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                Assert.That(step.Accepted, Is.True);
            }
            Assert.That(round.TurnIndex, Is.EqualTo(2));
            Assert.That(round.TryCommitCard(wasp, Red(WaspCardContent.LiftUp).Id).Accepted,
                Is.True);
            var revealed = round.TryCommitCard(nebkher,
                Blue(NebKherCardContent.PhantasmalSentry).Id);
            Assert.That(revealed.Accepted, Is.True);
            Assert.That(round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn));
            Assert.That(round.CurrentTurnAuthority.IsStarted, Is.False);
            Assert.That(round.CurrentTurnAuthority.PendingEffectChoice, Is.Not.Null);

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, payload,
                setup.CardDefinitions, new[] { map });
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload));
            var choice = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(choice.Candidates.Select(value => value.Value.HeroId),
                Is.EqualTo(new[] { wasp }));
            var selected = choice.SelectInvocations.Single();
            var target = restored.Round.TryAnswerEffectChoice(selected);
            Assert.That(target.Accepted, Is.True, target.RejectionReason.ToString());
            choice = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(choice.Candidates.Select(value => value.Value),
                Has.Member(EffectValue.Card(gold.Id)));
            var discarded = restored.Round.TryAnswerEffectChoice(
                choice.SelectInvocations.Single(value =>
                    value.Candidate.Value.Equals(EffectValue.Card(gold.Id))));
            Assert.That(discarded.Accepted, Is.True, discarded.RejectionReason.ToString());
            Assert.That(restored.Cards.Single(card => card.Id.Equals(gold.Id))
                .State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(restored.Round.CurrentTurnAuthority.IsStarted, Is.True);
        }
    }
}

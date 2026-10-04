using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
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
    public sealed class EmmittTimeWarpOutcomeTests
    {
        [Test]
        public void EnemySeatChoosesItsPriorResolvedCard_AndTheExchangeSurvivesRestore()
        {
            var red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var participants = StagedBoardParticipants.Create();
            var definitions = new[]
            {
                Plain("red-first", 1), Plain("blue-resolved", 12),
                Plain("red-warp", 12), Plain("blue-waiting", 1)
            };
            CardInstance InHand(int index) => CardInstance.CreateInHand(
                new CardInstanceId(definitions[index].Id.Value), definitions[index].Reference,
                index == 0 || index == 2 ? red : blue);
            var cards = Enumerable.Range(0, 4).Select(InHand).ToArray();
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(-1, 0), new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0) },
                new[]
                {
                    new HeroOccupancy(red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue, new HexCoordinate(1, 0))
                });
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var round = new RoundResolutionAuthority(
                definitions, cards, participants, new TieBreakerState(TeamSide.Red), 1,
                new RoundResolutionComposition(
                    new CardActionResolutionAuthority(catalogs, board), board,
                    new ActiveEffectLifecycle(), new MarkerAttachmentRegister(),
                    null, null, null, null, null, null));

            Assert.That(round.TryCommitCard(red, cards[0].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blue, cards[1].Id).Accepted, Is.True);
            for (var guard = 0; round.Phase == RoundResolutionPhase.ResolvingTurn && guard < 8; guard++)
            {
                var turn = round.CurrentTurnAuthority;
                var step = turn.PendingTurnRelease != null
                    ? round.TryReleaseTurn()
                    : round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                Assert.That(step.Accepted, Is.True);
            }
            Assert.That(round.TurnIndex, Is.EqualTo(2));
            Assert.That(CardLifecycleAuthority.IsResolvedCardInTurnArea(cards[1]), Is.True);

            Assert.That(round.TryCommitCard(red, cards[2].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blue, cards[3].Id).Accepted, Is.True);
            Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(red));
            var handler = catalogs.Capabilities.Load(EmmittTimeWarpContent.SwapWithResolvedV1);
            var context = new EffectCapabilityContext(
                new EffectNodeId("time-warp-swap-outcome"), EffectFailurePolicy.Mandatory,
                red, red, cards[2].Id, definitions[2].Reference,
                new ActionDefinitionId("primary-skill"),
                new Dictionary<string, EffectValue>
                {
                    [EmmittTimeWarpContent.OwnerParameter] = EffectValue.Hero(blue)
                },
                1, 2, participants, round.Timing, RuleModificationSet.Empty,
                EffectCardState.Of(definitions, cards), ActionKind.Skill,
                rosterState: EffectRosterState.Of(round.TurnSlots,
                    new RoundHeroDefeatHistory(participants), new HeroCoinLedger(participants),
                    unresolvedCardSwap: round));
            var offered = handler.Execute(context);
            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.ChooserHeroId, Is.EqualTo(blue));
            Assert.That(offered.Choice.Candidates, Is.EqualTo(new[] { EffectValue.Card(cards[1].Id) }));
            Assert.That(handler.Resume(context, new[] { EffectValue.Card(cards[1].Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(CardLifecycleAuthority.IsResolvingCardInTurnArea(cards[1]), Is.True);
            Assert.That(CardLifecycleAuthority.IsResolvedCardInTurnArea(cards[3]), Is.True);
            Assert.That(round.TurnSlots[0].Assignments.Single(value => value.HeroId.Equals(blue))
                .CardInstanceId, Is.EqualTo(cards[3].Id));
            Assert.That(round.TurnSlots[1].Assignments.Single(value => value.HeroId.Equals(blue))
                .CardInstanceId, Is.EqualTo(cards[1].Id));
            Assert.That(handler.Resume(context, new[] { EffectValue.Card(cards[1].Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "the former resolved-card answer is stale after the transaction");

            var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot,
                definitions, new[] { board.MapDefinition });
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(snapshot));
        }

        private static CardDefinition Plain(string slug, int initiative) => new CardDefinition(
            new CardDefinitionId("emmitt-time-warp-" + slug), "fixture-rev-1", slug,
            new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });
    }
}

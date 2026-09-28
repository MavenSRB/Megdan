using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114g. Wuk's decisions over the private-online wire, on the staged boards his Play
    /// Mode journeys load. Every decision addressed to a seat other than Wuk's - the trampled Wasp's
    /// discard, Arien's retrieval under Tree of Plenty, Red's removal after Dominance's exclusion - is
    /// drawn on that seat and on no other, is answered by that seat's own command, and leaves every
    /// seat's owner-scoped projection agreeing with the authority.
    /// </summary>
    public sealed class WukNetworkSeatTests
    {
        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate WukSpace = H(-3, 1);

        private static DirectorCommand[] Commands(NetworkMatchClient seat) =>
            seat.Projection.DecisionContract?.Commands.ToArray() ?? new DirectorCommand[0];

        private sealed class Wire
        {
            public Wire(WukStagedSession staged)
            {
                Staged = staged;
                Host = new MatchHost(staged.Session);
                Harness = PrivateOnlineWireHarness.Compose(Host);
                Harness.SeatAll();
            }

            public WukStagedSession Staged { get; }
            public MatchHost Host { get; }
            public PrivateOnlineWireHarness Harness { get; }

            public NetworkMatchClient SeatOf(HeroId hero)
            {
                var seat = Host.Seats.Seats.Single(value => value.HeroId.Equals(hero));
                return new NetworkMatchClient(Harness.Client(Harness.OrdinalOf(seat.Id.Value)),
                    Host.Projection, MatchContentCatalogs.ForThisBuild());
            }

            public void Pump()
            {
                Harness.Pump();
                Harness.Pump();
            }

            /// <summary>Every seat's projection agrees with the authority on version, pieces and Trees.</summary>
            public void AssertEverySeatAgrees(string moment)
            {
                Pump();
                foreach (var seat in Host.Seats.Seats)
                {
                    var view = SeatOf(seat.HeroId).Projection;
                    Assert.That(view.Version, Is.EqualTo(Host.Projection.Version), moment + " " + seat.Id.Value);
                    Assert.That(view.Heroes.Select(hero => (hero.HeroId.Value, hero.Coordinate)).OrderBy(pair => pair.Value),
                        Is.EqualTo(Host.Projection.Heroes.Select(hero => (hero.HeroId.Value, hero.Coordinate))
                            .OrderBy(pair => pair.Value)), moment + " " + seat.Id.Value);
                    Assert.That(view.PlacedTokens.Select(token => token.Coordinate).OrderBy(space => space.Q)
                            .ThenBy(space => space.R),
                        Is.EqualTo(Host.Projection.PlacedTokens.Select(token => token.Coordinate)
                            .OrderBy(space => space.Q).ThenBy(space => space.R)),
                        moment + ": seat " + seat.Id.Value + " draws the Trees the authority holds.");
                }
            }
        }

        /// <summary>Turn 1: Mystic Saplings plants one Tree beside Wuk and declines the rest.</summary>
        private static void PlantOneTree(WukStagedSession staged)
        {
            staged.CommitSo(staged.Wuk, WukCardContent.MysticSaplings);
            staged.UntilActs(staged.Wuk);
            staged.Start(ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary);
            staged.Select(value => value.Kind == EffectValueKind.Coordinate &&
                HexDistanceQuery.Calculate(WukSpace, value.Coordinate) == 1);
            while (staged.Projection.PendingEffectChoice != null)
            {
                staged.Accept(DirectorCommand.AnswerEffectChoice(
                    staged.Projection.PendingEffectChoice.DeclineInvocation));
            }

            Assert.That(staged.Trees(), Has.Length.EqualTo(1));
            staged.UntilCommitments();
        }

        [Test]
        public void ATrampleDiscard_RestoresAndIsAnsweredOnlyByTheTrampledSeat()
        {
            var staged = WukStagedSession.Create(
                new[] { WukRedFaces.Trample, WukCardContent.MysticSaplings }, WukSpace, H(-2, 1));
            staged.CommitSo(staged.Wuk, WukRedFaces.Trample);
            staged.UntilActs(staged.Wuk);
            staged.Start(ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary);
            var walk = staged.Projection.PendingMovementChoice;
            Assert.That(walk, Is.Not.Null);
            staged.Accept(DirectorCommand.ChooseMovementDestination(walk.ResolutionId, walk.ChoiceId, H(-1, 1)));
            var rest = staged.Projection.PendingMovementChoice;
            if (rest != null) staged.Accept(DirectorCommand.StopMovement(rest.ResolutionId, rest.ChoiceId));
            var discard = staged.Projection.PendingEffectChoice;
            Assert.That(discard?.ChooserId, Is.EqualTo(staged.Wasp), "Wasp, moved through, chooses her discard.");

            var snapshot = staged.Session.CaptureSnapshot();
            staged.Select(value => value.Kind == EffectValueKind.CardInstance);
            Assert.That(staged.Projection.PendingEffectChoice, Is.Null);
            Assert.That(staged.Session.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(staged.Projection.PendingEffectChoice?.NodeId, Is.EqualTo(discard.NodeId),
                "The restored match stands at the same question, the walk read back from the board.");
            Assert.That(staged.Session.CaptureSnapshot(), Is.EqualTo(snapshot));

            var wire = new Wire(staged);
            wire.AssertEverySeatAgrees("At the trample's discard");
            Assert.That(Commands(wire.SeatOf(staged.Wuk)).Any(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "Wuk's seat draws no control for Wasp's discard.");
            var waspSeat = wire.SeatOf(staged.Wasp);
            var before = wire.Host.Projection.Cards.Count(card =>
                card.OwnerId.Equals(staged.Wasp) && card.State.Location == CardLocation.Hand);
            var pick = Commands(waspSeat).First(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(waspSeat.Submit(pick).Accepted, Is.True);
            wire.Pump();
            var confirm = Commands(wire.SeatOf(staged.Wasp)).FirstOrDefault(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm != null)
            {
                Assert.That(wire.SeatOf(staged.Wasp).Submit(confirm).Accepted, Is.True);
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.Cards.Count(card =>
                card.OwnerId.Equals(staged.Wasp) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(before - 1), "Wasp discarded from her own seat.");
            wire.AssertEverySeatAgrees("After the discard");
        }

        [Test]
        public void TreeOfPlentysFriendlyRetrieval_IsAnsweredOnArienSeat()
        {
            var staged = WukStagedSession.Create(
                new[] { WukCardContent.MysticSaplings, WukBlueFaces.TreeOfPlenty }, WukSpace, H(0, 1),
                arienAt: H(-3, 2));
            PlantOneTree(staged);
            staged.CommitSo(staged.Wuk, WukBlueFaces.TreeOfPlenty);
            staged.UntilActs(staged.Wuk);
            staged.Start(ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary);
            staged.Select(value => value.Kind == EffectValueKind.Coordinate);
            var modal = staged.Projection.PendingEffectChoice;
            staged.Accept(DirectorCommand.AnswerEffectChoice(modal.SelectInvocations.Single(invocation =>
                invocation.Candidate.OptionNodeId.Value == WukBlueFaces.FriendBulletNode)));
            if (staged.Projection.PendingEffectChoice?.NodeId.Equals(modal.NodeId) == true)
                staged.Accept(DirectorCommand.AnswerEffectChoice(
                    staged.Projection.PendingEffectChoice.ConfirmInvocation));
            staged.Select(value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(staged.Arien));
            Assert.That(staged.Projection.PendingEffectChoice?.ChooserId, Is.EqualTo(staged.Arien),
                "Arien answers his own retrieval.");

            var wire = new Wire(staged);
            wire.AssertEverySeatAgrees("At Arien's retrieval");
            Assert.That(Commands(wire.SeatOf(staged.Wuk)).Any(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "Wuk's seat draws no control for Arien's choice.");
            var before = wire.Host.Projection.Cards.Count(card =>
                card.OwnerId.Equals(staged.Arien) && card.State.Location == CardLocation.Hand);
            for (var step = 0; step < 4 && wire.Host.Projection.PendingEffectChoice != null; step++)
            {
                var seat = wire.SeatOf(staged.Arien);
                var command = Commands(seat).FirstOrDefault(value =>
                        value.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        value.EffectChoice.Kind == EffectChoiceCommandKind.Select) ??
                    Commands(seat).First(value => value.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        value.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
                Assert.That(seat.Submit(command).Accepted, Is.True);
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.Cards.Count(card =>
                card.OwnerId.Equals(staged.Arien) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(before + 1), "Arien retrieved from his own seat.");
            wire.AssertEverySeatAgrees("After the retrieval");
        }

        [Test]
        public void DominancesExclusion_IsChosenOnWuksSeat_AndRedsRemovalOnWaspsSeat()
        {
            var staged = WukStagedSession.Create(
                new[] { WukBlueFaces.ClaimDominance, WukCardContent.MysticSaplings }, WukSpace, H(0, 1));
            staged.CommitSo(staged.Wuk, WukBlueFaces.ClaimDominance);
            staged.UntilActs(staged.Wuk);
            staged.Start(ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary);
            for (var step = 0; step < 80 && staged.Projection.Phase != Core.Cards.RoundResolutionPhase.EndOfRound; step++)
            {
                var projection = staged.Projection;
                if (projection.Phase == Core.Cards.RoundResolutionPhase.AwaitingCommitments)
                {
                    foreach (var hero in projection.Participants.Heroes.Select(value => value.Id).ToArray())
                    {
                        if (staged.Projection.Phase != Core.Cards.RoundResolutionPhase.AwaitingCommitments) break;
                        if (staged.Session.Execute(DirectorCommand.Pass(hero)).Accepted) continue;
                        var card = staged.Projection.Cards.FirstOrDefault(value =>
                            value.OwnerId.Equals(hero) && value.State.Location == CardLocation.Hand);
                        if (card != null) staged.Session.Execute(DirectorCommand.CommitCard(hero, card.Id));
                    }

                    continue;
                }

                if (projection.PendingInitiativeChoice != null)
                {
                    staged.Accept(DirectorCommand.ChooseTiedActor(projection.PendingInitiativeChoice.OwningSide,
                        projection.PendingInitiativeChoice.CandidateCardInstanceIds[0]));
                    continue;
                }

                staged.EndTurn();
            }

            staged.Accept(DirectorCommand.AdvanceEndOfRound());
            var exclusion = staged.Projection.PendingEffectChoice;
            Assert.That(exclusion?.NodeId.Value, Is.EqualTo(WukDominanceContent.ExcludeNode));

            var wire = new Wire(staged);
            wire.AssertEverySeatAgrees("At the pre-battle exclusion");
            Assert.That(Commands(wire.SeatOf(staged.Wasp)).Any(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "Wasp's seat draws no control for Wuk's exclusion.");
            var wukSeat = wire.SeatOf(staged.Wuk);
            var pick = Commands(wukSeat).First(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Minion);
            Assert.That(wukSeat.Submit(pick).Accepted, Is.True);
            wire.Pump();
            var confirm = Commands(wire.SeatOf(staged.Wuk)).FirstOrDefault(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm != null)
            {
                Assert.That(wire.SeatOf(staged.Wuk).Submit(confirm).Accepted, Is.True);
                wire.Pump();
            }

            for (var step = 0; step < 4 && wire.Host.Projection.PendingMinionBattleChoice == null; step++)
            {
                staged.Session.Execute(DirectorCommand.AdvanceEndOfRound());
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.PendingMinionBattleChoice, Is.Not.Null,
                "Red, one short after the exclusion, must remove a minion.");
            Assert.That(Commands(wire.SeatOf(staged.Wasp)), Is.Not.Empty,
                "Red's removal is drawn on Wasp's seat.");
            Assert.That(Commands(wire.SeatOf(staged.Wuk)), Is.Empty,
                "and not on Wuk's.");
            wire.AssertEverySeatAgrees("At Red's removal");
        }
    }
}

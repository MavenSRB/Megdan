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
    /// Milestone 8.112h. Mrak's decisions over the private-online wire, on the staged board his Play
    /// Mode journeys load. Each decision is drawn on the seat it addresses and on no other, is
    /// answered by that seat's own command, and leaves every seat's owner-scoped projection agreeing
    /// with the authority. Carried lessons 5 (a multi-pick is completed by the seat, not skipped) and
    /// 7 (discards addressed to another hero, and the Carapace's substitute, on the addressed seat).
    /// </summary>
    public sealed class MrakNetworkSeatTests
    {
        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate MrakSpace = H(-3, 1);

        /// <summary>The commands a seat may submit now; a seat nothing is addressed to has none.</summary>
        private static DirectorCommand[] Commands(NetworkMatchClient seat) =>
            seat.Projection.DecisionContract?.Commands.ToArray() ?? new DirectorCommand[0];

        private sealed class Wire
        {
            public Wire(MrakStagedSession staged)
            {
                Staged = staged;
                Host = new MatchHost(staged.Session);
                Harness = PrivateOnlineWireHarness.Compose(Host);
                Harness.SeatAll();
            }

            public MrakStagedSession Staged { get; }
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

            /// <summary>Every seat's projection agrees with the authority on version, pieces and rocks.</summary>
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
                        moment + ": seat " + seat.Id.Value + " draws the rocks the authority holds.");
                }
            }
        }

        [Test]
        public void APausedSecondaryAttack_RestoresAndIsAnsweredOnlyByTheDefendersSeat()
        {
            var staged = MrakStagedSession.Create(
                new[] { MrakRedFaces.SeismicSlam, MrakCardContent.Fissure }, MrakSpace, H(-2, 1));
            staged.CommitSo(staged.Mrak, MrakRedFaces.SeismicSlam);
            staged.UntilActs(staged.Mrak);
            staged.Start(ActionKind.Attack, ActionDefinitionOrigin.PrintedSecondary);
            staged.TargetHero(staged.Wasp);
            var pause = staged.Projection.PendingDefenseChoice;
            Assert.That(pause, Is.Not.Null, "Seismic Slam's printed secondary Attack waits at Wasp's Defense.");

            // Snapshot/restore of the configured match at the paused secondary Attack.
            var snapshot = staged.Session.CaptureSnapshot();
            staged.Accept(DirectorCommand.DeclineDefense(pause.ResolutionId, pause.ChoiceId));
            Assert.That(staged.Projection.Heroes.Any(hero => hero.HeroId.Equals(staged.Wasp)), Is.False);
            Assert.That(staged.Session.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(staged.Projection.PendingDefenseChoice?.ResolutionId, Is.EqualTo(pause.ResolutionId),
                "The restored match stands at the same paused secondary Attack.");
            Assert.That(staged.Projection.Heroes.Any(hero => hero.HeroId.Equals(staged.Wasp)), Is.True);
            Assert.That(staged.Session.CaptureSnapshot(), Is.EqualTo(snapshot));

            var wire = new Wire(staged);
            wire.AssertEverySeatAgrees("At the paused secondary Attack");
            var mrakSeat = wire.SeatOf(staged.Mrak);
            Assert.That(Commands(mrakSeat).Any(command =>
                    command.Kind == DirectorCommandKind.ChooseDefenseCard ||
                    command.Kind == DirectorCommandKind.DeclineDefense), Is.False,
                "The attacker's seat draws no Defense control.");
            var waspSeat = wire.SeatOf(staged.Wasp);
            Assert.That(waspSeat.Projection.PendingDefenseChoice?.ResolutionId, Is.EqualTo(pause.ResolutionId));
            var decline = Commands(waspSeat).Single(command =>
                command.Kind == DirectorCommandKind.DeclineDefense);
            Assert.That(waspSeat.Submit(decline).Accepted, Is.True);
            wire.Pump();
            Assert.That(wire.Host.Projection.Heroes.Any(hero => hero.HeroId.Equals(staged.Wasp)), Is.False,
                "Wasp took the printed secondary Attack of 6 undefended.");
            wire.AssertEverySeatAgrees("After the secondary Attack");
        }

        [Test]
        public void SeismicSlamsDiscard_IsDrawnAndAnsweredOnWaspsOwnSeat()
        {
            var staged = MrakStagedSession.Create(
                new[] { MrakRedFaces.SeismicSlam, MrakCardContent.Fissure }, MrakSpace, H(-3, 2));
            staged.CommitSo(staged.Mrak, MrakRedFaces.SeismicSlam);
            staged.UntilActs(staged.Mrak);
            staged.Start(ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary);

            var wire = new Wire(staged);
            var mrakSeat = wire.SeatOf(staged.Mrak);
            var select = Commands(mrakSeat).Single(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Hero &&
                command.EffectChoice.Candidate.Value.HeroId.Equals(staged.Wasp));
            Assert.That(mrakSeat.Submit(select).Accepted, Is.True);
            wire.Pump();

            var discard = wire.Host.Projection.PendingEffectChoice;
            Assert.That(discard?.ChooserId, Is.EqualTo(staged.Wasp), "Wasp chooses the card he discards.");
            Assert.That(Commands(wire.SeatOf(staged.Mrak)).Any(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "Mrak's seat draws no control for Wasp's discard.");
            var waspSeat = wire.SeatOf(staged.Wasp);
            var card = Commands(waspSeat).First(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Value.Kind == EffectValueKind.CardInstance);
            var discarded = card.EffectChoice.Candidate.Value.CardInstanceId;
            Assert.That(waspSeat.Submit(card).Accepted, Is.True);
            wire.Pump();
            var standing = waspSeat.Projection.PendingEffectChoice;
            if (standing != null && standing.ConfirmInvocation != null)
            {
                Assert.That(waspSeat.Submit(Commands(waspSeat).Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm)).Accepted, Is.True);
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.Cards.Single(value => value.Id.Equals(discarded)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            wire.AssertEverySeatAgrees("After Wasp's discard");
        }

        [Test]
        public void TheCarapacesSubstitute_IsOfferedOnMraksSeatForADiscardWaspsShockDemands()
        {
            var staged = MrakStagedSession.Create(
                new[] { MrakRedFaces.StoneCarapace, MrakGreenFaces.TreacherousGround, MrakCardContent.Fissure },
                MrakSpace, H(-1, 1),
                new[] { WaspCardContent.Shock, WaspCardContent.StopProjectiles, WaspCardContent.LiftUp },
                H(-1, 2));
            staged.CommitSo(staged.Mrak, MrakRedFaces.StoneCarapace);
            staged.UntilActs(staged.Mrak);
            staged.Start(ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary);
            staged.EndTurn();
            staged.UntilCommitments();
            staged.Accept(DirectorCommand.CommitCard(staged.Wasp, staged.Card(staged.Wasp, WaspCardContent.Shock).Id));
            staged.Accept(DirectorCommand.CommitCard(staged.Mrak,
                staged.Card(staged.Mrak, MrakGreenFaces.TreacherousGround).Id));
            staged.UntilActs(staged.Wasp);
            staged.Start(ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary);
            staged.TargetMinion(MrakStagedSession.StagedMinion);
            staged.Select(value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(staged.Mrak));

            var carapace = staged.Card(staged.Mrak, MrakRedFaces.StoneCarapace);
            Assert.That(carapace.State.Location, Is.EqualTo(CardLocation.TurnArea));
            var wire = new Wire(staged);
            Assert.That(Commands(wire.SeatOf(staged.Wasp)).Any(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "Wasp's seat draws no control for the discard it demanded of Mrak.");
            var mrakSeat = wire.SeatOf(staged.Mrak);
            var offered = Commands(mrakSeat).Where(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select).ToArray();
            Assert.That(offered.Select(command => command.EffectChoice.Candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { staged.Card(staged.Mrak, MrakCardContent.Fissure).Id, carapace.Id }),
                "The resolved Carapace is offered beside the one card in Mrak's hand, on Mrak's seat.");
            Assert.That(mrakSeat.Submit(offered.Single(command =>
                command.EffectChoice.Candidate.Value.CardInstanceId.Equals(carapace.Id))).Accepted, Is.True);
            wire.Pump();
            var standing = mrakSeat.Projection.PendingEffectChoice;
            if (standing != null && standing.ConfirmInvocation != null)
            {
                Assert.That(mrakSeat.Submit(Commands(mrakSeat).Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm)).Accepted, Is.True);
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.Cards.Single(card => card.Id.Equals(carapace.Id)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(wire.Host.Projection.Cards.Single(card =>
                    card.Id.Equals(staged.Card(staged.Mrak, MrakCardContent.Fissure).Id)).State.Location,
                Is.EqualTo(CardLocation.Hand), "The hand card is kept.");
            wire.AssertEverySeatAgrees("After the substitute discard");
        }

        [Test]
        public void FissuresTwoOfThreeReclaim_IsCompletedByMraksSeatAndEverySeatDrawsTheRocks()
        {
            // Stone Grip's three rocks stand; Fissure's line toward the board edge holds two empty
            // spaces, so p.18 takes back two of the three and Mrak chooses which (min 2, max 2).
            var staged = MrakStagedSession.Create(
                new[] { MrakCardContent.StoneGrip, MrakCardContent.Fissure, MrakGreenFaces.TreacherousGround },
                H(-1, 0), H(0, 0));
            staged.CommitSo(staged.Mrak, MrakCardContent.StoneGrip);
            staged.UntilActs(staged.Mrak);
            staged.Start(ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary);
            staged.Select(value => value.Kind == EffectValueKind.Hero);
            Assert.That(staged.Rocks(), Is.EquivalentTo(new[] { H(0, 1), H(1, -1), H(1, 0) }));
            staged.UntilCommitments();
            staged.CommitSo(staged.Mrak, MrakCardContent.Fissure);
            staged.UntilActs(staged.Mrak);
            staged.Start(ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary);
            staged.TargetHero(staged.Wasp);
            var defense = staged.Projection.PendingDefenseChoice;
            staged.Accept(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            var reclaim = staged.Projection.PendingEffectChoice;
            Assert.That(reclaim?.NodeId.Value, Is.EqualTo("fissure-reclaim"));
            Assert.That(reclaim.MinimumSelections, Is.EqualTo(2));
            Assert.That(reclaim.MaximumSelections, Is.EqualTo(2));

            var wire = new Wire(staged);
            wire.AssertEverySeatAgrees("At the reclaim");
            var mrakSeat = wire.SeatOf(staged.Mrak);
            foreach (var space in new[] { H(0, 1), H(1, -1) })
            {
                var select = Commands(mrakSeat).Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                    command.EffectChoice.Candidate.Value.Coordinate.Equals(space));
                Assert.That(mrakSeat.Submit(select).Accepted, Is.True);
                wire.Pump();
                _ = mrakSeat.Projection;
                wire.Pump();
            }

            var filled = mrakSeat.Projection.PendingEffectChoice;
            if (filled != null && filled.NodeId.Value == "fissure-reclaim")
            {
                Assert.That(filled.SelectedCandidates, Has.Count.EqualTo(2));
                Assert.That(mrakSeat.Submit(Commands(mrakSeat).Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm)).Accepted, Is.True);
                wire.Pump();
            }

            Assert.That(wire.Host.Projection.PlacedTokens.Select(token => token.Coordinate),
                Is.EquivalentTo(new[] { H(1, 0), H(0, 0), H(2, 0) }),
                "The unchosen rock stays; the two reclaimed rocks fill the line's two empty spaces.");
            wire.AssertEverySeatAgrees("After Fissure's line");
        }
    }
}

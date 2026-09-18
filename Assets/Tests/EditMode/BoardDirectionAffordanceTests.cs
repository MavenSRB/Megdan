using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.46d. A declared <see cref="EffectChoiceValueVocabulary.HexDirection"/> choice is
    /// answered ON THE BOARD: six arrow affordances around the hero being asked, one per offered
    /// direction, each submitting the same Select its prompt control submits.
    ///
    /// <para>Driven on Tali's Cold Snap, which is the only content in the build that opens the
    /// vocabulary, and on a real staged match rather than a stub: the whole claim is that the
    /// candidates, the spaces and the commands all come from one authoritative pause, so a fixture
    /// that assembled any of the three itself would be proving its own arithmetic.</para>
    ///
    /// <para>Excluded from the offline harness by name in <c>Tools/DotnetTests/UnityOnlyTests.props</c>
    /// because it names <see cref="BoardInteractionPresenter"/>, and the harness's Presentation slice
    /// compiles one file.</para>
    /// </summary>
    public sealed class BoardDirectionAffordanceTests
    {
        /// <summary>
        /// The six directions, drawn around the hero being asked and standing exactly one step away
        /// each - which is what makes an arrow clickable through the board's existing destination
        /// path rather than through any input plumbing of its own.
        /// </summary>
        [Test]
        public void ADirectionChoice_OffersOneArrowPerDirectionOnTheChoosersOwnNeighbours()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var choice = board.Session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null,
                "Cold Snap's \"Choose a direction\" opens unconditionally.");
            Assert.That(choice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.HexDirection),
                "The arrows exist because the handler DECLARED what its integers mean.");

            var model = Render(board);
            var chooserAt = board.Session.Projection.Heroes
                .Single(hero => hero.HeroId.Equals(choice.ChooserId)).Coordinate;

            Assert.That(model.DirectionChooserSpace, Is.EqualTo(chooserAt),
                "The arrows are drawn around the hero the choice names, read off the board rather " +
                "than off the selected actor.");
            Assert.That(model.DirectionCandidates.Select(value => value.Direction),
                Is.EquivalentTo(Enum.GetValues(typeof(HexDirection)).Cast<HexDirection>()),
                "All six of p.17's directions are always offered, so all six get an arrow.");
            foreach (var candidate in model.DirectionCandidates)
            {
                Assert.That(candidate.Neighbour,
                    Is.EqualTo(chooserAt.GetNeighbor(candidate.Direction)),
                    "An arrow stands on the space one step that way.");
                Assert.That(candidate.Label, Is.EqualTo(EffectChoiceLabels.VocabularyLabel(
                        EffectChoiceValueVocabulary.HexDirection, (int)candidate.Direction)),
                    "and wears the same words the prompt control wears.");
            }
        }

        /// <summary>
        /// Each arrow carries the standing contract's OWN command for its direction - never one this
        /// surface composed, and never one decoded back out of a coordinate.
        /// </summary>
        [Test]
        public void EachArrow_CarriesTheContractsOwnSelectForThatDirection()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var choice = board.Session.Projection.PendingEffectChoice;
            var contract = board.Session.Projection.DecisionContract;
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.FamilyId, Is.EqualTo(PendingDecisionFamilies.Effect));

            var model = Render(board);

            foreach (var candidate in model.DirectionCandidates)
            {
                var invocation = choice.SelectInvocations.Single(value =>
                    value.Candidate.Value.IntegerValue == (int)candidate.Direction);
                Assert.That(candidate.Command.Kind,
                    Is.EqualTo(DirectorCommandKind.AnswerEffectChoice));
                Assert.That(contract.Commands, Has.Member(candidate.Command),
                    "The command is the standing contract's, so the host authorizes exactly what " +
                    "the board offered.");
                Assert.That(candidate.Command.EffectChoice, Is.SameAs(invocation),
                    "and it is the select for THAT direction, matched by identity rather than by " +
                    "rebuilding a payload.");
            }

            Assert.That(model.DirectionCandidates.Select(value => value.Command).Distinct().Count(),
                Is.EqualTo(model.DirectionCandidates.Count));
        }

        /// <summary>
        /// The press. One board click on an arrow answers the direction outright - the select and the
        /// confirm the authority offers immediately after it - so no second question follows.
        /// </summary>
        [Test]
        public void ClickingAnArrow_AnswersTheDirectionInOnePress()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var presenter = Presenter(board, out var view);
            presenter.Refresh();
            var chosen = view.Model.DirectionCandidates.First();

            Assert.That(presenter.SubmitDestination(chosen.Neighbour), Is.True,
                "The arrow's own space answers the choice.");

            Assert.That(board.Session.Projection.PendingEffectChoice, Is.Null,
                "A choice of exactly one resolves on the press: nothing is left holding an " +
                "unconfirmed direction.");
            presenter.Refresh();
            Assert.That(view.Model.DirectionCandidates, Is.Empty,
                "and the arrows are cleared with the pause that raised them.");
            Assert.That(view.Model.DirectionChooserSpace, Is.Null);
        }

        /// <summary>
        /// A space that carries no arrow answers nothing, which is what keeps the direction route a
        /// route rather than a catch-all: the rest of the board still means what it meant.
        /// </summary>
        [Test]
        public void ASpaceWithNoArrow_AnswersNothing()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var presenter = Presenter(board, out var view);
            presenter.Refresh();
            var arrows = view.Model.DirectionCandidates.Select(value => value.Neighbour).ToArray();
            var elsewhere = view.Model.DirectionChooserSpace.Value;

            Assert.That(arrows, Has.No.Member(elsewhere),
                "The chooser's own space is not one of its neighbours.");
            Assert.That(presenter.SubmitDestination(elsewhere), Is.False);
            Assert.That(board.Session.Projection.PendingEffectChoice, Is.Not.Null,
                "and the question is still standing.");
        }

        /// <summary>
        /// The seat that does not own the decision draws no arrows - and, just as importantly, does
        /// not throw deriving them. A non-owner is published no effect choice and no decision
        /// contract at all, and <c>PendingDecisionCommandBinding.Require</c> faults on a null
        /// contract, so the ownership question has to be asked before a single command is bound.
        /// </summary>
        [Test]
        public void ASeatThatDoesNotOwnTheDecision_DrawsNoArrows()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var seats = MatchSeatBinding.ForParticipants(board.Session.Projection.Participants);
            var chooser = board.Session.Projection.PendingEffectChoice.ChooserId;
            var other = seats.Seats.Single(seat => !seat.Owns(chooser));
            var scoped = SeatProjectionFactory.ForSeat(board.Session.Projection, other);

            Assert.That(scoped.PendingEffectChoice, Is.Null,
                "The fixture only proves this while the other seat really is a non-owner.");
            Assert.That(scoped.DecisionContract, Is.Null);

            var view = new CapturingBoardView();
            new BoardInteractionPresenter(new ReadOnlyClient(scoped), view).Refresh();

            Assert.That(view.Model.DirectionCandidates, Is.Empty);
            Assert.That(view.Model.DirectionChooserSpace, Is.Null);

            // And the owner, on the same projection, still gets all six - so the emptiness above is
            // this seat's scope rather than the derivation being broken.
            var owning = SeatProjectionFactory.ForSeat(
                board.Session.Projection, seats.Seats.Single(seat => seat.Owns(chooser)));
            var ownerView = new CapturingBoardView();
            new BoardInteractionPresenter(new ReadOnlyClient(owning), ownerView).Refresh();
            Assert.That(ownerView.Model.DirectionCandidates, Has.Count.EqualTo(6));
        }

        /// <summary>A board with no direction question standing carries no arrows at all.</summary>
        [Test]
        public void WithNoDirectionQuestionStanding_ThereAreNoArrows()
        {
            var board = TaliBoard.Create();
            var model = Render(board);

            Assert.That(model.DirectionCandidates, Is.Empty);
            Assert.That(model.DirectionChooserSpace, Is.Null);
        }

        /// <summary>
        /// The drawing half: one arrow per candidate, standing on that candidate's own space and
        /// pointing away from the chooser, generated on first use and released with the board.
        ///
        /// <para>The candidates are the real ones derived above; the board under them is synthetic,
        /// because what is being proved here is where the arrows are put and what they cost, and a
        /// staged board's own pieces would only add views for this fixture to own.</para>
        /// </summary>
        [Test]
        public void TheArrows_AreDrawnOnTheirOwnSpacesAndReleasedWithTheBoard()
        {
            var board = TaliBoard.Create();
            board.StartPrimary(TaliCardContent.ColdSnap);
            var derived = Render(board);
            var chooser = derived.DirectionChooserSpace.Value;
            var tiles = new[] { chooser }
                .Concat(derived.DirectionCandidates.Select(value => value.Neighbour))
                .Select(Tile)
                .ToArray();
            var view = new BoardInteractionUnityView(
                Array.Empty<GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView>(),
                tiles,
                null);
            owned.Add(view);

            view.Render(DrawingModel(derived.DirectionChooserSpace, derived.DirectionCandidates));

            var drawn = view.DirectionArrows.Where(value => value.IsVisible).ToArray();
            Assert.That(drawn, Has.Length.EqualTo(6));
            Assert.That(view.OwnedNativeResourceCount, Is.EqualTo(4),
                "Two meshes and two materials, however many arrows point at them.");
            foreach (var arrow in drawn)
            {
                var space = HexWorldLayout.ToWorld(arrow.Space, HexWorldLayout.DefaultOuterRadius);
                Assert.That(arrow.transform.localPosition.x, Is.EqualTo(space.x).Within(0.0001f));
                Assert.That(arrow.transform.localPosition.z, Is.EqualTo(space.z).Within(0.0001f),
                    "An arrow stands on the space its own candidate names.");
                var outward = (space - HexWorldLayout.ToWorld(
                    chooser, HexWorldLayout.DefaultOuterRadius)).normalized;
                Assert.That(
                    Vector3.Dot(arrow.transform.localRotation * Vector3.right, outward),
                    Is.GreaterThan(0.999f),
                    "and points away from the chooser, which is the whole of what an arrow says.");
            }

            view.Render(DrawingModel(null, Array.Empty<DirectionCandidateViewModel>()));
            Assert.That(view.DirectionArrows.Where(value => value.IsVisible), Is.Empty,
                "The pause has resolved, so nothing is drawn - and the arrows are retained rather " +
                "than destroyed, ready for the next question.");
            Assert.That(view.DirectionArrows, Has.Count.EqualTo(6));

            view.ReleaseOwnedResources();
            Assert.That(view.OwnedNativeResourceCount, Is.Zero);
            Assert.That(view.DirectionArrows, Is.Empty);
        }

        /// <summary>
        /// The narrowest board that carries direction arrows: no pieces, no cues, nothing else to
        /// draw. Every other member is what an empty board looks like.
        /// </summary>
        private static BoardInteractionViewModel DrawingModel(
            HexCoordinate? chooserSpace,
            System.Collections.Generic.IEnumerable<DirectionCandidateViewModel> candidates) =>
            new BoardInteractionViewModel(
                1L,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                directionChooserSpace: chooserSpace,
                directionCandidates: candidates);

        private HexTileView Tile(HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, HexWorldLayout.DefaultOuterRadius);
            return tile;
        }

        private readonly System.Collections.Generic.List<object> owned =
            new System.Collections.Generic.List<object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var value in owned)
            {
                (value as BoardInteractionUnityView)?.ReleaseOwnedResources();
            }

            foreach (var value in owned)
            {
                if (value is UnityEngine.Object created && created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created);
                }
            }

            owned.Clear();
        }

        private static BoardInteractionViewModel Render(StagedHeroBoard board)
        {
            Presenter(board, out var view).Refresh();
            return view.Model;
        }

        private static BoardInteractionPresenter Presenter(
            StagedHeroBoard board, out CapturingBoardView view)
        {
            view = new CapturingBoardView();
            return new BoardInteractionPresenter(board.Session, view);
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }

            public void Render(BoardInteractionViewModel model) => Model = model;
        }

        /// <summary>
        /// One seat's own view, read-only. It answers <c>Refresh()</c> and nothing else, because the
        /// claim about a non-owner is exactly that it has nothing to submit.
        /// </summary>
        private sealed class ReadOnlyClient : IMatchClient
        {
            public ReadOnlyClient(IMatchReadModel projection) => Projection = projection;

            public IMatchReadModel Projection { get; }

            public bool IsDirector => false;

            public MatchClientResult Submit(DirectorCommand command) =>
                throw new NotSupportedException("This seat double answers reads only.");

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                throw new NotSupportedException("This seat double answers reads only.");

            public MatchClientResult SubmitUndo() =>
                throw new NotSupportedException("This seat double answers reads only.");
        }
    }
}

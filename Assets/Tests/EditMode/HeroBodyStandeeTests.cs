using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21i. A many-bodied hero on the board: one standee per standing body with its quiet
    /// per-body mark, cues and the pick per body, the acting cue on the engine's acting body, and a
    /// removed body fading like a Defeat. The presenter cases run on 9.21a's staged two-body board
    /// (<see cref="HeroBodiesBoard"/>); the view cases on hand-built pieces, as the other board-view
    /// fixtures are.
    /// </summary>
    public sealed class HeroBodyStandeeTests
    {
        [Test]
        public void ThePick_CuesEveryStandingBody_AndAClickOnAStandeeAnswersItWithThatBody()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out var red, out var blue);
            var board = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, board);
            presenter.Refresh();

            var cues = board.Model.PieceTargetCues.Where(cue => cue.Target.HeroId.Equals(blue)).ToArray();
            Assert.That(cues.Select(cue => cue.Target.HeroBody), Is.EquivalentTo(new[] { 1, 2 }),
                "Each standing body is cued on its own standee, named by its body.");
            Assert.That(cues.All(cue => cue.State == BoardTargetCueState.Eligible &&
                cue.ConsequenceIntent == BoardTargetConsequenceIntent.Ally), Is.True);
            Assert.That(board.Model.PieceTargetCues.Any(cue => cue.Target.HeroId.Equals(red)), Is.False);
            Assert.That(board.Model.FocusedHeroId, Is.EqualTo(blue));
            Assert.That(board.Model.FocusedHeroBody, Is.EqualTo(0),
                "No body acts while the pick stands, so the acting cue stands on none.");

            Assert.That(presenter.SubmitHeroBody(blue, 3), Is.False, "Body 3 does not stand.");
            Assert.That(presenter.SubmitHeroBody(red, 1), Is.False, "The pick is his, not Red's.");
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Not.Null);

            Assert.That(presenter.SubmitHeroBody(blue, 2), Is.True);
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null);
            Assert.That(session.Projection.ActingBodies, Is.EqualTo(new[] { new HeroActingBody(blue, 2) }));
            presenter.Refresh();
            Assert.That(board.Model.FocusedHeroBody, Is.EqualTo(2),
                "The acting cue is read from the engine's settled acting body.");
            Assert.That(board.Model.PieceTargetCues.Any(cue =>
                cue.ConsequenceIntent == BoardTargetConsequenceIntent.Ally), Is.False);
        }

        [Test]
        public void ThePick_SubmitsExactlyTheContractsCommands_OnePerStandee()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue);
            var recorder = new RecordingSession(session.Projection);
            var presenter = new BoardInteractionPresenter(recorder, new CapturingBoardView());
            foreach (var body in session.Projection.PendingActingBodyChoice.Bodies)
            {
                Assert.That(presenter.SubmitHeroBody(body.HeroId, body.Body), Is.True);
            }

            Assert.That(recorder.Commands, Is.EquivalentTo(session.Projection.DecisionContract.Commands));
            Assert.That(recorder.Commands.Select(command => command.HeroBody), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(recorder.Commands.All(command =>
                command.Kind == DirectorCommandKind.ChooseActingBody && command.HeroId.Equals(blue)), Is.True);
        }

        [Test]
        public void AnAttack_CuesEachOfferedBody_AndTheClickedStandeeNamesItsBody()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out _, out var blue, out _);
            var board = new CapturingBoardView();
            var recorder = new RecordingSession(session.Projection);
            var presenter = new BoardInteractionPresenter(recorder, board);
            presenter.Refresh();

            var cues = board.Model.PieceTargetCues.Where(cue => cue.Target.HeroId.Equals(blue)).ToArray();
            Assert.That(cues.Select(cue => cue.Target.HeroBody), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(cues.All(cue => cue.ConsequenceIntent == BoardTargetConsequenceIntent.Attack), Is.True);
            Assert.That(board.Model.AttackTargetDefensePreviews.Where(preview => preview.HeroId.Equals(blue))
                .Select(preview => preview.HeroBody), Is.EquivalentTo(new[] { 1, 2 }),
                "One Defense preview per body is one board model, not a refused duplicate.");

            Assert.That(presenter.SubmitHero(blue), Is.False,
                "A click naming only the hero cannot say which body is attacked.");
            Assert.That(recorder.Commands, Is.Empty);
            Assert.That(presenter.SubmitHeroBody(blue, 2), Is.True);
            Assert.That(recorder.Commands.Single().Kind, Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(recorder.Commands.Single().HeroBody, Is.EqualTo(2));
            Assert.That(recorder.Commands.Single(), Is.EqualTo(session.Projection.DecisionContract.Commands
                .Single(command => command.HeroId.Equals(blue) && command.HeroBody == 2)));
        }

        [Test]
        public void TheBoard_DrawsAStandeePerBody_WithItsMark_AndTheActingCueOnTheActingBody()
        {
            var objects = new List<GameObject>();
            var red = new HeroId("red-hero");
            var blue = new HeroId("blue-hero");
            var redAt = new HeroOccupancy(red, new HexCoordinate(0, 0));
            var blueOne = new HeroOccupancy(blue, 1, new HexCoordinate(2, 0));
            var blueTwo = new HeroOccupancy(blue, 2, new HexCoordinate(0, 2));
            var redView = CreateHero(objects, redAt, TeamSide.Red);
            var blueView = CreateHero(objects, blueOne, TeamSide.Blue);
            var view = new BoardInteractionUnityView(new[] { redView, blueView }, Array.Empty<HexTileView>(), null);
            var created = new List<PrimitiveHeroView>();
            view.HeroBodyViewCreated += created.Add;
            var treatment = BoardTargetCuePresentation.Treatment;
            try
            {
                BoardTargetCuePresentation.Treatment = BoardTargetCueTreatment.RingOnThePiece;
                view.Render(Model(1, new[] { redAt, blueOne, blueTwo }, blue, 2,
                    new PieceTargetCueViewModel(BoardObjectReference.ForHeroBody(blue, 2),
                        BoardTargetCueState.Eligible, BoardTargetConsequenceIntent.Ally)));

                Assert.That(created, Has.Count.EqualTo(1), "Body 2 is drawn once, the first time it stands.");
                var bodyTwo = created[0];
                Assert.That(view.HeroViews, Has.Count.EqualTo(3));
                Assert.That(bodyTwo.Occupancy, Is.EqualTo(blueTwo));
                Assert.That(bodyTwo.TeamSide, Is.EqualTo(TeamSide.Blue), "The same hero's team face.");
                Assert.That(bodyTwo.GetComponent<PrimitiveHeroPointerSelector>(), Is.Not.Null);
                Assert.That(bodyTwo.transform.parent, Is.SameAs(blueView.transform.parent));

                Assert.That(blueView.BodyMark.VisiblePips, Is.EqualTo(1));
                Assert.That(bodyTwo.BodyMark.VisiblePips, Is.EqualTo(2));
                Assert.That(redView.BodyMark == null || !redView.BodyMark.IsVisible, Is.True,
                    "A hero on one space wears no body mark: every single-bodied piece is unchanged.");

                Assert.That(view.ActingHeroMarker.IsVisible, Is.True);
                Assert.That(Flat(view.ActingHeroMarker.transform.position),
                    Is.EqualTo(Flat(bodyTwo.transform.position)), "The acting cue stands on body 2.");
                Assert.That(bodyTwo.TargetCueState, Is.EqualTo(BoardTargetCueState.Eligible));
                Assert.That(blueView.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                    "A cue naming body 2 is body 2's alone.");

                // The engine moves the acting body (a "different you" repeat): the cue follows it.
                view.Render(Model(2, new[] { redAt, blueOne, blueTwo }, blue, 1));
                Assert.That(Flat(view.ActingHeroMarker.transform.position),
                    Is.EqualTo(Flat(blueView.transform.position)));

                // No body settled (the pick stands): the ring stands on no body.
                view.Render(Model(3, new[] { redAt, blueOne, blueTwo }, blue, 0));
                Assert.That(view.ActingHeroMarker.IsVisible, Is.False);

                // Body 2 removed without a Defeat: its standee fades like a Defeat's, body 1 stays.
                view.Render(Model(4, new[] { redAt, blueOne }, blue, 1));
                Assert.That(bodyTwo.gameObject.activeSelf, Is.False);
                Assert.That(blueView.gameObject.activeSelf, Is.True);
                Assert.That(view.HeroDefeatGhostFor(bodyTwo), Is.Not.Null);
                Assert.That(view.HeroDefeatGhostFor(blueView), Is.Null);
                Assert.That(blueView.BodyMark.IsVisible, Is.False,
                    "Body 1 alone stands again, so it reads as the one piece it is.");

                // Spawned again, the same standee returns; the Defeat then fades every body.
                view.Render(Model(5, new[] { redAt, blueOne, blueTwo }, red, 0));
                Assert.That(bodyTwo.gameObject.activeSelf, Is.True);
                Assert.That(view.HeroViews, Has.Count.EqualTo(3), "A returning body reuses its standee.");
                view.Render(Model(6, new[] { redAt }, red, 0));
                Assert.That(view.HeroDefeatGhostFor(bodyTwo), Is.Not.Null);
                Assert.That(view.HeroDefeatGhostFor(blueView), Is.Not.Null);
            }
            finally
            {
                BoardTargetCuePresentation.Treatment = treatment;
                if (view.ActingHeroMarker != null) objects.Add(view.ActingHeroMarker.gameObject);
                objects.AddRange(view.HeroViews.Where(value => value != null).Select(value => value.gameObject));
                DestroyAll(objects);
            }
        }

        [Test]
        public void AClickOnAStandee_CarriesItsBody_AndHoverLightsThatBodyOnly()
        {
            var objects = new List<GameObject>();
            var blue = new HeroId("blue-hero");
            var blueOne = new HeroOccupancy(blue, 1, new HexCoordinate(0, 0));
            var blueTwo = new HeroOccupancy(blue, 2, new HexCoordinate(3, 0));
            var blueView = CreateHero(objects, blueOne, TeamSide.Blue);
            var view = new BoardInteractionUnityView(new[] { blueView }, Array.Empty<HexTileView>(), null);
            try
            {
                view.Render(Model(1, new[] { blueOne, blueTwo }, default, 0));
                var bodyTwo = view.HeroViews.Single(value => value.Body == 2);
                var clicked = new List<(HeroId, int)>();
                var heroOnly = new List<HeroId>();
                view.BindIntentHandlers(
                    hero => { heroOnly.Add(hero); return true; },
                    _ => false,
                    _ => false,
                    null,
                    (hero, body) => { clicked.Add((hero, body)); return true; });

                var selector = bodyTwo.GetComponent<PrimitiveHeroPointerSelector>();
                Assert.That(selector.TrySelectFromPointerRay(DownRayAt(bodyTwo.transform.position)), Is.True);
                Assert.That(clicked, Is.EqualTo(new[] { (blue, 2) }));
                Assert.That(heroOnly, Is.Empty, "A standee's click goes to the body-aware handler.");

                Assert.That(selector.TryGetHeroFromPointerRay(
                    DownRayAt(bodyTwo.transform.position), out var hovered, out var hoveredBody), Is.True);
                Assert.That((hovered, hoveredBody), Is.EqualTo((blue, 2)));
            }
            finally
            {
                objects.AddRange(view.HeroViews.Where(value => value != null).Select(value => value.gameObject));
                DestroyAll(objects);
            }
        }

        private static Vector2 Flat(Vector3 position) =>
            new Vector2((float)Math.Round(position.x, 3), (float)Math.Round(position.z, 3));

        private static BoardInteractionViewModel Model(
            long version, IReadOnlyList<HeroOccupancy> heroes, HeroId focused, int focusedBody,
            params PieceTargetCueViewModel[] cues) =>
            new BoardInteractionViewModel(
                version,
                heroes,
                Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                focused,
                pieceTargetCues: cues,
                focusedHeroBody: focusedBody);

        private static PrimitiveHeroView CreateHero(
            ICollection<GameObject> objects, HeroOccupancy occupancy, TeamSide side)
        {
            var gameObject = new GameObject($"Hero {occupancy.HeroId}");
            objects.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            SetTeam(hero, side);
            hero.Configure(occupancy, HexWorldLayout.DefaultOuterRadius);
            hero.EnsureVisual();
            gameObject.AddComponent<PrimitiveHeroPointerSelector>();
            return hero;
        }

        private static void SetTeam(PrimitiveHeroView hero, TeamSide side) =>
            typeof(PrimitiveHeroView)
                .GetField("teamSide", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(hero, side);

        private static Ray DownRayAt(Vector3 target) => new Ray(target + (Vector3.up * 10f), Vector3.down);

        private static void DestroyAll(IEnumerable<GameObject> objects)
        {
            foreach (var value in objects.Distinct())
            {
                if (value != null) Object.DestroyImmediate(value);
            }
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }

        private sealed class RecordingSession : IMatchSession
        {
            private readonly List<DirectorCommand> commands = new List<DirectorCommand>();
            public RecordingSession(MatchSessionProjection projection) { Projection = projection; }
            public MatchSessionProjection Projection { get; }
            public IReadOnlyList<DirectorCommand> Commands => commands;
            public MatchCommandResult Execute(DirectorCommand command)
            {
                commands.Add(command);
                return new MatchCommandResult(true, command, MatchSessionRejectionReason.None,
                    string.Empty, default, default, default, default,
                    Array.Empty<RoundResolutionEvent>(), Projection);
            }
            public MatchSessionOperationResult Undo() => throw new NotSupportedException();
            public MatchSessionOperationResult Restart() => throw new NotSupportedException();
            public MatchSessionOperationResult RestoreSnapshot(string payload) =>
                throw new NotSupportedException();
            public string CaptureSnapshot() => throw new NotSupportedException();
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z4. The seat-scoped presenters' viewer identity, and the fan-out that binds it.
    ///
    /// <para><b>What actually broke, and why nothing caught it.</b> A networked client cannot know
    /// its hero when the scene composes: the lobby grants the seat afterwards. So
    /// <c>ConfiguredMatchSceneRuntime</c> constructs every seat-scoped presenter with
    /// <c>Seat?.HeroId ?? default</c> - the UNDEFINED hero - and repairs them at admission. Before
    /// this milestone it repaired exactly one of the three, and the other two could not be repaired
    /// even in principle: their identity sat in <c>readonly</c> fields.</para>
    ///
    /// <para>The undefined hero is not a neutral blank. It is the DIRECTOR'S identity, and the
    /// Director legitimately owns every decision at once, so every "is this mine?" predicate reads an
    /// unrepaired seat as "yes, everything is mine". On the first live internet playtest that meant
    /// every hero's turn release narrated in the first person ("YOUR TURN — RELEASE TO CONTINUE"),
    /// the gold awaited-bar lit through other players' commitments, a greyed END TURN was drawn on
    /// seats that were not acting, and the board-mark pulse took the wrong branch.</para>
    ///
    /// <para><b>Why the existing narration cases could never have caught it.</b> They hand the
    /// presenter the CORRECT viewer id and check the sentence - so they check the predicate, which
    /// was never wrong. The defect was in the composition: which id the live runtime actually passes,
    /// and whether anything later tells it the truth. That is what this fixture asserts, and it
    /// asserts it STRUCTURALLY, so a fourth seat-scoped presenter cannot be added and forgotten the
    /// same way.</para>
    /// </summary>
    public sealed class SeatScopedPresenterBindingTests
    {
        /// <summary>
        /// Every presenter that consumes a viewer identity must declare it by implementing
        /// <see cref="ISeatScopedPresenter"/>. A presenter that takes a <see cref="HeroId"/> in its
        /// constructor and does NOT implement the interface is exactly the shape that shipped: it can
        /// be composed with the undefined hero and has no way to be told better.
        /// </summary>
        [Test]
        public void EveryPresenterConstructedWithAViewerIdentity_DeclaresItselfSeatScoped()
        {
            var offenders = typeof(RoundActionPresenter).Assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Presenter"))
                .Where(type => type.GetConstructors().Any(constructor =>
                    constructor.GetParameters().Any(parameter =>
                        parameter.ParameterType == typeof(HeroId))))
                .Where(type => !typeof(ISeatScopedPresenter).IsAssignableFrom(type))
                .Select(type => type.Name)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "A presenter taking a viewer HeroId is seat-scoped whether it says so or not. " +
                "Implement ISeatScopedPresenter and join ConfiguredMatchSceneRuntime's fan-out, or " +
                "a networked client will compose it with the undefined hero and never correct it.");
        }

        /// <summary>
        /// The fan-out's coverage, checked without composing a Unity scene: every
        /// <see cref="ISeatScopedPresenter"/> implementation must be reachable as a property of the
        /// runtime, which is what <c>SeatScopedPresenters</c> is composed from.
        /// </summary>
        [Test]
        public void EverySeatScopedPresenter_IsExposedByTheRuntimeThatMustBindIt()
        {
            var runtime = typeof(ConfiguredMatchSceneRuntime);
            var exposed = runtime
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.PropertyType)
                .ToArray();

            var implementations = typeof(RoundActionPresenter).Assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract &&
                    typeof(ISeatScopedPresenter).IsAssignableFrom(type))
                .ToArray();

            Assert.That(implementations, Is.Not.Empty, "The interface must have implementations.");
            foreach (var implementation in implementations)
            {
                Assert.That(exposed, Contains.Item(implementation),
                    implementation.Name + " is seat-scoped but the runtime does not expose it, so " +
                    "BindHomeHero cannot reach it and its viewer identity would stay undefined.");
            }

            Assert.That(
                runtime.GetProperty(nameof(ConfiguredMatchSceneRuntime.SeatScopedPresenters)),
                Is.Not.Null,
                "The one list that answers 'who must be told the admitted seat's hero'.");
        }

        /// <summary>
        /// The roster, pinned. This is a tripwire rather than a truth: adding a further seat-scoped
        /// presenter SHOULD fail here, so that whoever adds it visits
        /// <c>ConfiguredMatchSceneRuntime.BindHomeHero</c> deliberately instead of discovering the
        /// omission on a playtest.
        ///
        /// <para><b>It did exactly that at milestone 8.35z8's integration, which is the first
        /// evidence this tripwire works.</b> The buttons/fizzle branch was written against
        /// `983aa205`, before milestone 8.35z4 existed, so its new <see
        /// cref="FizzleNoticePresenter"/> took a viewer <c>HeroId</c> into a <c>readonly</c> field
        /// and the runtime composed it with `Seat?.HeroId ?? default` - the same shape, in a fourth
        /// place, as the two presenters 8.35z4 had to repair. Left alone it would have shown every
        /// seat an acknowledge-to-continue notice for every OTHER player's blocked action, because
        /// the stream reads an undefined viewer as the Director, who owns every hero's fizzle. The
        /// presenter now implements the interface and the roster is four.</para>
        /// </summary>
        [Test]
        public void TheSeatScopedRoster_IsExactlyThePresentersTheFanOutBinds()
        {
            var roster = typeof(RoundActionPresenter).Assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract &&
                    typeof(ISeatScopedPresenter).IsAssignableFrom(type))
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(roster, Is.EqualTo(new[]
            {
                nameof(DirectorHandsPresenter),
                nameof(FizzleNoticePresenter),
                nameof(KineticCuePresenter),
                nameof(RoundActionPresenter)
            }), "If this list changed, ConfiguredMatchSceneRuntime.SeatScopedPresenters must have " +
                "changed with it - that is the whole point of pinning it here.");
        }

        /// <summary>
        /// The binding itself, at the tier the runtime uses it: a presenter composed the way a
        /// networked client composes one - with the undefined hero, because the seat does not exist
        /// yet - is told the seat's hero afterwards and reports it.
        /// </summary>
        [Test]
        public void APresenterComposedBeforeItsSeatExists_ReportsTheAdmittedHeroAfterBinding()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));

            // Exactly what ConfiguredMatchSceneRuntime's constructor does before the lobby admits
            // the player: `Seat?.HeroId ?? default`.
            var presenters = new ISeatScopedPresenter[]
            {
                new RoundActionPresenter(client, new CapturingRoundView(), default),
                new KineticCuePresenter(client, new CountingKineticView(), default),
                new DirectorHandsPresenter(client, new CapturingHandsView(), default)
            };

            foreach (var presenter in presenters)
            {
                Assert.That(presenter.HomeHeroId.IsDefined, Is.False,
                    presenter.GetType().Name + " starts undefined, which is the Director's own " +
                    "identity - and the reason an unrepaired seat reads every decision as its own.");
                Assert.That(presenter.SetHomeHero(seat.HeroId), Is.True,
                    presenter.GetType().Name + " must accept the admitted seat's own hero.");
                Assert.That(presenter.HomeHeroId, Is.EqualTo(seat.HeroId),
                    presenter.GetType().Name + " must report the hero it was bound to. This is the " +
                    "assertion the shipped composition would have failed for two of the three.");
            }
        }

        /// <summary>
        /// The refusal half: a hero this build does not have is not bound, so a half-bound HUD -
        /// some panels reading as the seat, others as the Director - stays unreachable.
        /// </summary>
        [Test]
        public void ABindingForAHeroThisBuildDoesNotHave_IsRefusedRatherThanStored()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
            var presenter = new RoundActionPresenter(client, new CapturingRoundView(), default);

            Assert.That(presenter.SetHomeHero(default), Is.False,
                "The undefined hero is not a binding.");
            Assert.That(presenter.HomeHeroId.IsDefined, Is.False);
        }

        /// <summary>
        /// THE REPORTED DEFECT, at the read-model tier the worker-scope diagnosis reproduced it at:
        /// a turn release owned by hero A, read on hero B's seat. Bound, B reads A's name; unbound,
        /// B reads it as its own - which is the sentence the playtest could not get rid of.
        /// </summary>
        [Test]
        public void ANonOwningSeat_ReadsTheReleasingHerosNameOnceItsIdentityIsBound()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host, 1500, projection => projection.PendingTurnRelease != null);

            var release = host.Projection.PendingTurnRelease;
            Assert.That(release, Is.Not.Null,
                "The seeded walk must reach a turn-release pause, or this case proves nothing.");

            var releasing = release.ActorId;
            var other = host.Seats.Seats.First(value => !value.HeroId.Equals(releasing));
            var othersProjection = host.ProjectionForSeat(other.Id);
            Assert.That(othersProjection.PendingTurnRelease, Is.Not.Null,
                "8.35g publishes the release unscoped, so every seat carries it - which is why the " +
                "wrong identity mattered on every seat at once.");

            var bound = RoundActionPresenter.CreateViewModel(othersProjection, other.HeroId);
            // `default(HeroId)` spelled out: the bare `default` is ambiguous against the
            // offersRoundContinuation overload, and this argument is the whole point of the case.
            var unbound = RoundActionPresenter.CreateViewModel(othersProjection, default(HeroId));

            var releasingName = host.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(releasing)).DisplayName.ToUpperInvariant();

            Assert.That(bound.Status, Is.EqualTo(NarrationLabels.HeroAwaitingRelease(releasingName)),
                "A bound seat names the hero the pause is actually standing on.");
            Assert.That(bound.Status, Does.Not.StartWith("YOUR "),
                "and never claims another hero's pause as its own.");
            Assert.That(bound.EndTurnInvocation, Is.Null,
                "A seat that is not acting is offered no completion...");
            Assert.That(bound.EndTurnUnavailableReason, Is.Null,
                "...and no greyed END TURN banner either, which is drawn from a non-null reason.");

            Assert.That(unbound.Status, Is.EqualTo("YOUR TURN — RELEASE TO CONTINUE"),
                "The unbound reading is kept as evidence, not as a defect: this is the Director's " +
                "own correct branch, and it is precisely what every networked seat was getting.");
        }

        // Local stubs: every other fixture's capturing view is private to it, and this fixture
        // needs all three surfaces at once to bind all three presenters.
        private sealed class CountingKineticView : IKineticCueView
        {
            public void Render(KineticCueViewModel model) => Rendered++;

            public int Rendered { get; private set; }
        }

        private sealed class CapturingRoundView : IRoundActionView
        {
            public void Render(RoundActionViewModel model) => Model = model;

            public RoundActionViewModel Model { get; private set; }
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public void Render(DirectorHandsViewModel model) => Model = model;

            public DirectorHandsViewModel Model { get; private set; }
        }
    }
}

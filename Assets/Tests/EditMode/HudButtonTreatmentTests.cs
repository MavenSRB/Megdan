using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z8 (the EIGHTEENTH amendment): one shared button treatment - four states,
    /// a quick press dip, a smooth hover lift, and chrome that matches the host panel - proven per
    /// control kind the amendment names: END TURN, the seat's own UNDO, the lobby's HOST/JOIN,
    /// agreement choices, a Defense option, and the DIRECTOR drawer's UNDO/RESET/EVENT LOG.
    /// </summary>
    public sealed class HudButtonTreatmentTests
    {
        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        private static void AssertScaleMotionIsWired(VisualElement element, string label)
        {
            Assert.That(element.style.scale.value.value, Is.EqualTo(Vector3.one),
                label + " starts at its idle scale.");
            var properties = element.style.transitionProperty.value;
            Assert.That(properties, Is.Not.Null, label + " declares a USS transition.");
            Assert.That(properties.Any(property => property.Equals(new StylePropertyName("scale"))), Is.True,
                label + " eases its own scale rather than snapping between states.");
        }

        [Test]
        public void Lighten_EasesTowardWhiteAndKeepsAlpha()
        {
            var color = new Color(0.2f, 0.3f, 0.4f, 0.9f);

            var lit = MatchHudTheme.Lighten(color, 0.5f);

            Assert.That(lit.r, Is.GreaterThan(color.r));
            Assert.That(lit.g, Is.GreaterThan(color.g));
            Assert.That(lit.b, Is.GreaterThan(color.b));
            Assert.That(lit.a, Is.EqualTo(color.a));
            Assert.That(MatchHudTheme.Lighten(color, 1f).r, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Darken_EasesTowardBlackAndKeepsAlpha()
        {
            var color = new Color(0.5f, 0.6f, 0.7f, 0.8f);

            var dark = MatchHudTheme.Darken(color, 0.5f);

            Assert.That(dark.r, Is.LessThan(color.r));
            Assert.That(dark.g, Is.LessThan(color.g));
            Assert.That(dark.b, Is.LessThan(color.b));
            Assert.That(dark.a, Is.EqualTo(color.a));
            Assert.That(MatchHudTheme.Darken(color, 1f).r, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Control_DrawsTheSharedIdleChromeAndWiresMotionWithBackgroundTint()
        {
            var button = HudElements.Control("A CONTROL", () => { });

            Assert.That(button.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
            Assert.That(button.style.borderTopLeftRadius.value.value,
                Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
            Assert.That(button.style.borderTopColor.value, Is.EqualTo(MatchHudTheme.PanelBorder));
            AssertScaleMotionIsWired(button, "HudElements.Control");
        }

        [Test]
        public void ApplyButtonMotion_WithoutBackgroundTint_NeverTouchesAnAlreadySetBackground()
        {
            var element = new VisualElement();
            element.style.backgroundColor = Color.magenta;

            HudElements.ApplyButtonMotion(element);

            Assert.That(element.style.backgroundColor.value, Is.EqualTo(Color.magenta),
                "A card face or a custom-painted banner keeps its own background untouched; " +
                "only the scale motion is added.");
            AssertScaleMotionIsWired(element, "a scale-only control (card face / banner)");
        }

        [Test]
        public void EndTurn_DrawsWithScaleMotionOnTopOfItsOwnBannerCloth()
        {
            var view = new RoundActionUnityView();
            var invocation = new TypedInvocationViewModel(
                "END TURN", DirectorCommand.PerformHold(new CardInstanceId("card-1")));
            var model = new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), invocation, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>());

            view.Render(model);

            var control = Descendants(view.Root).Single(element => element.name == "gd-end-turn-control");
            AssertScaleMotionIsWired(control, "END TURN");
        }

        [Test]
        public void SeatUndo_DrawsWithScaleMotionOnTopOfItsOwnBannerCloth()
        {
            var view = new RoundActionUnityView();
            var model = new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), null, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>(),
                seatUndoAvailable: true);

            view.Render(model);

            var control = Descendants(view.Root).Single(element => element.name == "gd-seat-undo-control");
            AssertScaleMotionIsWired(control, "the seat's own UNDO");
        }

        [Test]
        public void AgreementChoice_DrawsTheSharedChromeAndWiresMotion()
        {
            var proposal = new TeamAgreementProposalId("initiative-ui");
            var hero = new HeroId("hero-1");
            var choice = new TeamAgreementChoiceViewModel(
                "HERO 1", hero, TeamAgreementCommand.Prefer(proposal, new CardInstanceId("card-1")));
            var agreement = new TeamAgreementHudViewModel(
                TeamAgreementKind.Initiative, "WHO ACTS FIRST?",
                new[] { new TeamAgreementParticipantViewModel("HERO 1", "is choosing") },
                new[] { choice });
            var model = new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), null, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>(),
                teamAgreement: agreement);
            var view = new RoundActionUnityView();

            view.Render(model);

            var control = Descendants(view.OptionalStepActionsRoot)
                .Single(element => element.name == "gd-initiative-preference");
            Assert.That(control.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
            Assert.That(control.style.borderTopLeftRadius.value.value,
                Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
            AssertScaleMotionIsWired(control, "an agreement choice");
        }

        [Test]
        public void DefenseOption_DrawsWithScaleMotionOverItsOwnCardFace()
        {
            var session = ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var controls = Descendants(view.Root)
                .Where(element => element.name == "gd-defense-card")
                .ToArray();
            Assert.That(controls, Is.Not.Empty, "Sanity: the fixture offers at least one card defense.");
            foreach (var control in controls)
            {
                AssertScaleMotionIsWired(control, "a Defense option");
            }
        }

        [Test]
        public void DirectorUndoAndRestart_DrawTheSharedChromeAndWireMotion()
        {
            var view = new UndoRestartUnityView();

            var undo = Descendants(view.Root).OfType<Button>().Single(button => button.text == "UNDO");
            var restart = Descendants(view.Root).OfType<Button>().Single(button => button.text == "RESET");

            foreach (var control in new[] { undo, restart })
            {
                Assert.That(control.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
                Assert.That(control.style.borderTopLeftRadius.value.value,
                    Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
                AssertScaleMotionIsWired(control, control.text);
            }
        }

        [Test]
        public void EventLogToggle_DrawsTheSharedChromeAndWiresMotion()
        {
            var view = new DiagnosticLogUnityView();

            Assert.That(view.ToggleRoot.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
            AssertScaleMotionIsWired(view.ToggleRoot, "EVENT LOG");
        }

        [Test]
        public void LobbyHostAndJoin_DrawTheSharedDarkChromeInsteadOfTheStockButtonSkin()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var model = PrivateLobbyModel.Entry(driver.Session.Roster, string.Empty);
            var view = new PrivateLobbyUnityView();

            view.Render(model);

            var controls = Descendants(view.Root)
                .OfType<Button>()
                .Where(button => button.name == PrivateLobbyUnityView.ControlName)
                .ToArray();
            Assert.That(controls.Select(button => button.text),
                Is.EquivalentTo(new[] { "HOST", "JOIN" }));
            foreach (var control in controls)
            {
                // Pre-fix, these controls carried none of this: Unity's own stock button skin,
                // unstyled against a hand-painted dark panel - exactly what read as "pasted over".
                Assert.That(control.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
                Assert.That(control.style.borderTopWidth.value, Is.GreaterThan(0f),
                    "The lobby's own controls now draw a real border rather than none at all.");
                Assert.That(control.style.borderTopLeftRadius.value.value,
                    Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
                AssertScaleMotionIsWired(control, control.text);
            }
        }

        [Test]
        public void LobbySeatPick_DrawsTheSharedChromeAndWiresMotion()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var model = PrivateLobbyModel.Entry(driver.Session.Roster, string.Empty);
            var view = new PrivateLobbyUnityView();

            view.Render(model);

            var picks = Descendants(view.Root)
                .OfType<Button>()
                .Where(button => button.name == PrivateLobbyUnityView.SeatPickName)
                .ToArray();
            Assert.That(picks, Is.Not.Empty);
            foreach (var pick in picks)
            {
                Assert.That(pick.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
                AssertScaleMotionIsWired(pick, "PICK");
            }
        }

        /// <summary>
        /// Coordinator-directed small QoL item (2026-09-05, PLAN.md's "SMALL QoL ITEM"): "which
        /// build am I running" is a glance at the lobby screen, after the user lost time to a stale
        /// package folder. The line renders <see cref="BuildIdentityStamp.Current"/> - the exact
        /// formula the admission tuple's own <c>BuildIdentity</c> element already reads (see
        /// <see cref="PrivateSessionIdentity"/>), so the glance and the packaged
        /// <c>BUILD-IDENTITY.txt</c> record can never disagree.
        /// </summary>
        [Test]
        public void LobbyBuildStamp_RendersTheSameFormulaTheAdmissionTupleReads()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var model = PrivateLobbyModel.Entry(driver.Session.Roster, string.Empty);
            var view = new PrivateLobbyUnityView();

            view.Render(model);

            var stamp = Descendants(view.Root)
                .OfType<Label>()
                .SingleOrDefault(label => label.name == PrivateLobbyUnityView.BuildStampName);
            Assert.That(stamp, Is.Not.Null, "The lobby screen carries exactly one build stamp line.");
            Assert.That(stamp.text, Is.EqualTo(BuildIdentityStamp.Current));
            Assert.That(stamp.text, Does.Contain(UnityEngine.Application.unityVersion),
                "The stamp names the Editor version, not only the product version.");
            Assert.That(stamp.pickingMode, Is.EqualTo(PickingMode.Ignore),
                "A watermark, never a control - it must not steal a click from the panel above it.");
        }
    }
}

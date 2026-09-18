using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
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
    /// <remarks>
    /// Milestone 8.35z58 (the re-flagged amendment, integration worktree) extends this
    /// coverage two ways: <c>LobbyHeroOption_...</c> closes the one control 8.35z37 added after
    /// this pass shipped and that never picked up the shared chrome, and
    /// <c>DisabledControl_KeepsItsTooltipReasonAlongsideTheSharedChrome</c> proves the disabled
    /// state and the SIXTH amendment's cursor-tooltip reason compose rather than fight.
    /// </remarks>
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
            // 8.35z58: the shared ButtonBorder, not the dimmer generic PanelBorder -
            // a button's own border must be visible, not merely present (see the contrast tests
            // below for why this distinction is pinned rather than assumed).
            Assert.That(button.style.borderTopColor.value, Is.EqualTo(MatchHudTheme.ButtonBorder));
            AssertScaleMotionIsWired(button, "HudElements.Control");
        }

        /// <summary>
        /// Milestone 8.35z58. Runtime tracing found 8.35z8's own treatment DID reach
        /// every control (composition and wiring were never the defect - confirmed by reading
        /// PrivateLobbyDriver.Start/BuildPanel end to end), but its chip (RGB ~0.145/0.169/0.184)
        /// sat only a shade brighter than the panels around it (RGB ~0.03-0.09), which is exactly
        /// the "barely-different gray" failure mode: present, but reading as nothing. This pins a
        /// concrete, meaningful contrast floor against the darkest panel in the HUD family so a
        /// future edit cannot quietly walk the chip back toward invisibility while every
        /// structural assertion above stays green.
        /// </summary>
        [Test]
        public void ButtonIdleBackground_IsMeaningfullyBrighterThanTheDarkestHudPanel()
        {
            var delta = MatchHudTheme.ButtonIdleBackground.r - MatchHudTheme.TopBarBackground.r;

            Assert.That(delta, Is.GreaterThan(0.1f),
                "A button must read as a raised control against the darkest panel it can sit on, " +
                "not merely a slightly different shade of the same near-black.");
        }

        /// <summary>
        /// Milestone 8.35z58. The corner radius the button family draws at is the SAME
        /// number the top strip's narration band and the match-log tray already share
        /// (<see cref="MatchStatusBarUnityView.CornerRadius"/>) - the user's own accepted band
        /// language - read from one place, not two literals that happen to agree today.
        /// </summary>
        [Test]
        public void ButtonCornerRadius_ReusesTheAcceptedBandLanguageFromOneSource()
        {
            Assert.That(MatchHudTheme.ButtonCornerRadius, Is.EqualTo(MatchHudTheme.BandCornerRadius));
            Assert.That(MatchStatusBarUnityView.CornerRadius, Is.EqualTo(MatchHudTheme.BandCornerRadius));
        }

        /// <summary>
        /// Milestone 8.35z58. The user asked for "~100ms" (both the original EIGHTEENTH
        /// amendment and the corrective re-stated it); 8.35z8 had shipped 80ms.
        /// </summary>
        [Test]
        public void ButtonPressTransitionMs_IsTheHundredMillisecondsAsked()
        {
            Assert.That(MatchHudTheme.ButtonPressTransitionMs, Is.EqualTo(100));
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

        /// <summary>
        /// Milestone 8.35z58. The SIXTH amendment's "nothing is greyed without saying
        /// why" and the EIGHTEENTH amendment's shared button treatment are two different
        /// amendments landing on the same control (the exact production shape inside
        /// <c>RoundActionUnityView.BuildEffectChoiceSection</c>'s unavailable-mode loop: a
        /// <see cref="HudElements.Control"/> carrying a tooltip, then marked unavailable). This
        /// proves the second never silently
        /// undoes the first: a disabled control keeps its tooltip reason, keeps the shared chrome
        /// wiring, and stays visibly dimmed - the fourth state, still distinct from idle/hover/
        /// pressed because the motion callbacks all gate on <c>enabledInHierarchy</c>.
        /// </summary>
        [Test]
        public void DisabledControl_KeepsItsTooltipReasonAlongsideTheSharedChrome()
        {
            var control = HudElements.Control("AN UNAVAILABLE MODE", null);
            control.tooltip = "NO TARGET";

            HudElements.MarkUnavailable(control);

            Assert.That(control.tooltip, Is.EqualTo("NO TARGET"),
                "Marking a control unavailable must not clear the reason a hover already carries.");
            Assert.That(control.enabledSelf, Is.False, "MarkUnavailable disables the control.");
            Assert.That(control.style.opacity.value, Is.EqualTo(0.45f).Within(0.0001f),
                "Unavailable is never colour alone - it is also visibly dimmed.");
            Assert.That(control.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground),
                "The shared chrome a Control() draws is untouched by disabling it.");
            AssertScaleMotionIsWired(control,
                "a disabled control (the transition stays wired; enabledInHierarchy gates it at hover time)");
        }

        [Test]
        public void EndTurn_DrawsTheSharedButtonChromeAndMotion()
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
            AssertSharedCornerChrome(control);
            AssertScaleMotionIsWired(control, "END TURN");
            var shine = control.Q("gd-end-turn-shine");
            Assert.That(shine, Is.Not.Null, "The sole remaining action keeps its cue.");
            Assert.That(shine.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void SeatUndo_DrawsTheSharedButtonChromeAndMotion()
        {
            var view = new RoundActionUnityView();
            var model = new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), null, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>(),
                seatUndoAvailable: true);

            view.Render(model);

            var control = Descendants(view.Root).Single(element => element.name == "gd-seat-undo-control");
            AssertSharedCornerChrome(control);
            AssertScaleMotionIsWired(control, "the seat's own UNDO");
        }

        /// <summary>
        /// Milestone 8.42d, and the CLOSING CHECK of the user's 2026-09-18 playtest report: a
        /// rewind the host has published is DRAWN, including while the acting seat's own card text
        /// is asking it a question.
        /// </summary>
        /// <remarks>
        /// <para>The report was "undo stops being available as soon as you select attack option on
        /// a card, before the target is selected", against Whisper. Her Crimson Trail is the
        /// measured case: the attack opens on <c>before-move</c>, one of whose six destinations
        /// carries a preview that names an object, so the presenter routes that candidate to
        /// <see cref="RoundActionViewModel.EffectChoiceTargets"/> - and
        /// <c>HasExclusiveDecision</c> then removed the square from the FIRST prompt of the attack.
        /// Brogan's charge reached the same place one click later, through the CONFIRM control a
        /// select raises into <see cref="RoundActionViewModel.EffectChoices"/>. The host published
        /// <see cref="IMatchReadModel.IsSeatUndoAvailable"/> throughout both and would have accepted
        /// the press; nothing but this view stood between the player and their own rewind.</para>
        /// <para>Both arms are asserted because they are two different routings of one prompt, and
        /// a fix that only reads one of the lists would leave the other card class dark.</para>
        /// </remarks>
        [Test]
        public void SeatUndo_StaysDrawnWhileTheActorsOwnCardTextChoiceStands()
        {
            var arms = new (string Name, RoundActionViewModel Model)[]
            {
                ("a card-text choice offering controls (Brogan's charge, after the select)",
                    SeatUndoModelWith(EffectChoiceSubject.Mode, effectChoices: new[]
                    {
                        new TypedInvocationViewModel(
                            "CONFIRM", DirectorCommand.PerformHold(new CardInstanceId("card-1")))
                    })),
                ("a card-text choice offering a named target (Whisper's Crimson Trail, at once)",
                    SeatUndoModelWith(EffectChoiceSubject.Piece, effectChoiceTargets: new[]
                    {
                        new EffectChoiceTargetViewModel(
                            "OPTION #1",
                            new HexCoordinate(-2, 1),
                            null,
                            DirectorCommand.PerformHold(new CardInstanceId("card-1")))
                    }))
            };

            foreach (var arm in arms)
            {
                var view = new RoundActionUnityView();
                view.Render(arm.Model);

                var control = Descendants(view.Root)
                    .SingleOrDefault(element => element.name == "gd-seat-undo-control");
                Assert.That(control, Is.Not.Null,
                    arm.Name + ": the host published this seat's rewind, so the square is drawn.");
                Assert.That(view.SeatUndoRoot.style.display.value,
                    Is.EqualTo(DisplayStyle.Flex),
                    arm.Name + ": its host surface is shown rather than collapsed.");
                AssertSharedCornerChrome(control);
            }
        }

        private static RoundActionViewModel SeatUndoModelWith(
            EffectChoiceSubject subject,
            IEnumerable<TypedInvocationViewModel> effectChoices = null,
            IEnumerable<EffectChoiceTargetViewModel> effectChoiceTargets = null) =>
            new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), null, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>(),
                effectChoices: effectChoices,
                effectChoiceTargets: effectChoiceTargets,
                effectChoicePrompt: new EffectChoicePromptViewModel(subject),
                seatUndoAvailable: true);

        [Test]
        public void DisabledEndTurn_KeepsTheSharedChromeAndItsReason()
        {
            var view = new RoundActionUnityView();
            view.Render(new RoundActionViewModel(
                1, 1, 1, RoundResolutionPhase.ResolvingTurn, string.Empty,
                Array.Empty<TypedInvocationViewModel>(), null, null,
                Array.Empty<TypedInvocationViewModel>(), Array.Empty<TypedInvocationViewModel>(),
                endTurnUnavailableReason: "Finish the pending choice first."));

            var control = view.EndTurnRoot.Q<Button>("gd-end-turn-control");
            Assert.That(control, Is.Not.Null);
            AssertSharedCornerChrome(control);
            Assert.That(control.enabledSelf, Is.False);
            Assert.That(control.tooltip, Is.EqualTo("Finish the pending choice first."));
            Assert.That(control.style.opacity.value, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(control.Q("gd-end-turn-shine"), Is.Null);
            AssertScaleMotionIsWired(control, "disabled END TURN");
        }

        private static void AssertSharedCornerChrome(VisualElement control)
        {
            Assert.That(control.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
            Assert.That(control.style.borderTopColor.value, Is.EqualTo(MatchHudTheme.ButtonBorder));
            Assert.That(control.style.borderTopLeftRadius.value.value,
                Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
            Assert.That(control.style.color.value, Is.EqualTo(MatchHudTheme.PrimaryText));
            Assert.That(control.Q("gd-banner-cloth"), Is.Null,
                "Old custom paint must not cover the shared button chrome.");
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

        /// <summary>
        /// Milestone <c>8.35z54</c> MOVED THIS TEST'S SUBJECT, and the button treatment itself is
        /// unchanged. The seat picker used to live on the ENTRY FORM, where a player chose a seat
        /// before joining and the button only remembered that choice for a JOIN that had not happened
        /// yet. The user's flow spec removed that surface: you join naming no seat, and the button
        /// that TAKES a seat appears once you are admitted and reading the host's own roster. So the
        /// entry form now correctly draws no seat picks at all, and rendering it here asserted the
        /// chrome of buttons that are no longer supposed to exist.
        ///
        /// <para>It renders the ADMITTED state instead - a real seatless client on the real wire
        /// harness, which is where a player meets these controls now - and asserts exactly what it
        /// always asserted about them.</para>
        /// </summary>
        [Test]
        public void LobbySeatPick_DrawsTheSharedChromeAndWiresMotion()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var joiner = harness.ConnectSeatless(1);
            harness.Pump();

            var model = PrivateLobbyModel.ForClient(joiner);
            var view = new PrivateLobbyUnityView();

            view.Render(model);

            var picks = Descendants(view.Root)
                .OfType<Button>()
                .Where(button => button.name == PrivateLobbyUnityView.SeatPickName)
                .ToArray();
            Assert.That(picks, Is.Not.Empty,
                "An admitted player is offered a TAKE SEAT control on every open seat - the state " +
                "the entry form used to carry. Status: " + model.StatusLabel + ".");
            foreach (var pick in picks)
            {
                Assert.That(pick.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
                AssertScaleMotionIsWired(pick, "PICK");
            }
        }

        /// <summary>
        /// Milestone 8.35z58. Rulebook p.22's hero-pick buttons
        /// (<see cref="PrivateLobbyUnityView.BuildHeroOption"/>) were added in milestone 8.35z37,
        /// AFTER 8.35z8's button-system pass had already routed every other lobby control through
        /// the shared chrome - so this one control was missed and kept drawing Unity's stock
        /// button skin with only a hand-rolled border, the exact defect 8.35z8 closed everywhere
        /// else. Driven over the real six-seat wire harness (the tier the shipped lobby defects
        /// actually lived in), through the seat/ready flow to the point rulebook p.22's pick order
        /// is genuinely open, rather than constructing the view-model by hand.
        /// </summary>
        [Test]
        public void LobbyHeroOption_DrawsTheSharedChromeAndWiresMotion()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                harness.Client(ordinal).TrySetReadiness(true);
            }

            harness.Pump();

            var model = PrivateLobbyModel.ForClient(harness.Client(1));
            Assert.That(model.Selection.IsRunning, Is.True,
                "Sanity: readying every seat opens rulebook p.22's pick order.");
            Assert.That(model.Selection.Heroes, Is.Not.Empty,
                "Sanity: the fixture's roster names heroes to offer.");

            var view = new PrivateLobbyUnityView();
            view.Render(model);

            var options = Descendants(view.Root)
                .OfType<Button>()
                .Where(button => button.name == PrivateLobbyUnityView.HeroOptionName)
                .ToArray();
            Assert.That(options, Is.Not.Empty);
            foreach (var option in options)
            {
                // Pre-fix, this control carried none of this: no themed background, no shared
                // radius, no press/hover motion - exactly what 8.35z8 fixed for HOST/JOIN/FILL/
                // LEAVE and the seat PICK, just on a control added one milestone after that pass.
                Assert.That(option.style.backgroundColor.value, Is.EqualTo(MatchHudTheme.ButtonIdleBackground));
                Assert.That(option.style.borderTopLeftRadius.value.value,
                    Is.EqualTo(MatchHudTheme.ButtonCornerRadius));
                AssertScaleMotionIsWired(option, "a hero-selection option");
            }
        }

        /// <summary>
        /// The other half of the move, asserted rather than assumed: the entry form draws NO seat
        /// picks. Without this, deleting the seat-pick control entirely would still leave the test
        /// above green on some other state, and the removed surface would have no proof it is gone.
        /// </summary>
        [Test]
        public void TheEntryForm_DrawsNoSeatPicksAtAll()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var model = PrivateLobbyModel.Entry(driver.Session.Roster, string.Empty);
            var view = new PrivateLobbyUnityView();

            view.Render(model);

            Assert.That(
                Descendants(view.Root)
                    .OfType<Button>()
                    .Where(button => button.name == PrivateLobbyUnityView.SeatPickName),
                Is.Empty,
                "Milestone 8.35z54 removed the pre-join seat picker: a seat is taken from inside " +
                "the session, against the host's roster, never guessed at before joining.");
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

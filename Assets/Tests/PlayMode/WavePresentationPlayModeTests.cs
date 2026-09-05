using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// WAVE ONE (milestones 8.35z11-8.35z14) on the real configured scene.
    ///
    /// <para>Both of this wave's presentation milestones were authored in worktrees, which have no
    /// Unity <c>Library/</c> and therefore no launcher, so each landed with a named Unity-owed list
    /// rather than with a Play Mode case. These are the two entries at the top of those lists - the
    /// ones their own notes call the largest remaining gaps - and they are here rather than in the
    /// Edit Mode fixtures because neither question can be asked of a hand-composed presenter.</para>
    ///
    /// <para><b>Why the anchor guard is worth a Play Mode case of its own.</b> Milestone 8.35z9 spent
    /// four milestones sweeping a shine over a set of host elements that was empty on the real HUD,
    /// and nothing failed - a sweep over nothing is a silent no-op. Milestone 8.35z11's card flight
    /// has exactly that shape: <c>KineticFlightAnchors</c> resolves every endpoint by QUERYING THE
    /// LIVE TREE for names the HUD gives its own elements, and not finding one is a supported outcome
    /// that skips the flight. So a rename anywhere in the HUD would cost the whole feature and no
    /// existing case would notice. Its own notes name this as "the case this pass most wants and
    /// could not write from a worktree".</para>
    /// </summary>
    public sealed class WavePresentationPlayModeTests
    {
        /// <summary>
        /// Milestone 8.35z11's owed guard: a COMPOSED HUD yields a non-empty anchor set for a hero.
        ///
        /// <para>Every name is asserted individually rather than as a count, because the failure this
        /// exists to catch is one anchor going missing while the others still resolve - which is
        /// precisely what a count would hide.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AComposedHud_YieldsAResolvableFlightAnchorSetForEveryHero()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hudRoot = root.Hud.RootElement;
            Assert.That(hudRoot, Is.Not.Null, "The composed HUD has a root to query.");

            var heroes = runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray();
            Assert.That(heroes.Length, Is.GreaterThan(0));

            foreach (var heroId in heroes)
            {
                var rail = KineticFlightAnchors.HeroRail(hudRoot, heroId);
                Assert.That(rail, Is.Not.Null,
                    "The rail for " + heroId.Value + " must be findable. Six rails share the name " +
                    "`gd-hero-rail`, so this resolves through the `rail.userData = hero.HeroId` " +
                    "stamp milestone 8.35z11 adds in BuildOverview - the single line it contributes " +
                    "to UiToolkitMatchHudViews. If this is null that line is gone or userData was " +
                    "reused, and every card flight to a particular hero silently stops.");

                Assert.That(KineticFlightAnchors.CurrentCardSlot(hudRoot, heroId), Is.Not.Null,
                    "The commitment arc's destination (`" +
                    KineticFlightAnchors.CurrentCardSlotName + "`) resolves for " + heroId.Value + ".");
                Assert.That(KineticFlightAnchors.Coins(hudRoot, heroId), Is.Not.Null,
                    "The coin flight's destination (`" + KineticFlightAnchors.HeroCoinsName +
                    "`) resolves for " + heroId.Value + ".");
                Assert.That(KineticFlightAnchors.Level(hudRoot, heroId), Is.Not.Null,
                    "The level pop's host (`" + KineticFlightAnchors.HeroLevelName +
                    "`) resolves for " + heroId.Value + ".");
            }

            Assert.That(KineticFlightAnchors.HandTray(hudRoot), Is.Not.Null,
                "and the hand tray (`" + KineticFlightAnchors.HandTrayName + "`), which is where the " +
                "viewer's own commitment arc sets out from, resolves once for the surface.");

            // The other half of the 8.35z9 lesson: an element that resolves but has never been laid
            // out has no usable centre, so a flight from it would set out from (0,0).
            var viewerRail = KineticFlightAnchors.HeroRail(hudRoot, heroes[0]);
            Assert.That(KineticSpaceBridge.IsPlaced(viewerRail), Is.True,
                "A resolved anchor is also LAID OUT - an element found but never placed would send " +
                "every flight from the panel's origin, which is the same silent no-op in a different " +
                "costume.");
        }

        /// <summary>
        /// Milestone 8.35z12's owed case, and its notes call it "the largest remaining gap": ONE
        /// spotlight on the real HUD, end to end.
        ///
        /// <para>It also proves the wave's conductor reconciliation on the real surface, which no
        /// Edit Mode case can: the stage does NOT self-grant any more - 8.35z11's conductor grants
        /// it - so if that registration were missing in the composition this journey would sit in
        /// Requested for ever and the assertion below would say so.</para>
        ///
        /// <para><b>The clock is driven explicitly.</b> A case that waited on wall-clock frames to
        /// cross a 0.45 s framing would be asserting the frame rate of whatever machine ran it. One
        /// real frame is spent first, and only to prove the scene's own driver is attached and
        /// advancing - nothing in this fixture ticks anything for that assertion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AResolvedAttack_OpensTheSpotlightOnTheRealHud_AndContinueGivesTheBoardBack()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);

            var spotlight = runtime.SpotlightView;
            var presenter = runtime.SpotlightPresenter;
            Assert.That(spotlight, Is.Not.Null,
                "The configured scene composes a spotlight surface like any other panel.");
            Assert.That(presenter, Is.Not.Null);

            var driver = Object.FindAnyObjectByType<AttackSpotlightDriver>();
            Assert.That(driver, Is.Not.Null,
                "The scene's per-frame spotlight driver is attached by the composition root.");
            Assert.That(driver.FailureCount, Is.EqualTo(0));

            Assert.That(presenter.Stage.AutoGrantsStage, Is.False,
                "THE RECONCILIATION, ASSERTED ON THE REAL COMPOSITION: the stage self-conducted while " +
                "it shipped standalone; milestone 8.35z11's conductor owns it now.");
            Assert.That(runtime.KineticView.Conductor.ConductedScene, Is.SameAs(presenter.Stage),
                "and it is THAT conductor's registered scene, so there is one conductor and not two.");

            Assert.That(spotlight.Model.Phase, Is.EqualTo(AttackSpotlightPhase.None),
                "The opening projection primes and frames nothing.");
            Assert.That(spotlight.IsChromeHidden, Is.False, "The HUD is up.");

            // ---- Close to adjacency, then attack, exactly as the kinetic journey does. ----
            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MagneticDagger);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, redAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);

            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True);
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40));
                AnswerAnyEndOfRoundPause(runtime);
            }

            Commit(runtime, TeamSide.Blue, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Red, WaspCardContent.LiftUp);
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, redHeroView.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            Assert.That(runtime.DeclineDefense(), Is.True);

            // ---- The Attack row exists now. The stage asks for the stage; the conductor grants it. ----
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Requested),
                "A resolved attack asks for the stage. It does NOT open by itself any more.");
            Assert.That(spotlight.IsChromeHidden, Is.False,
                "and it hides nothing at all while it waits - an ungranted scene changes nothing a " +
                "player can see.");

            runtime.KineticView.Tick(0f);
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Entering),
                "The conductor granted the stage on its own tick, through the registration above.");

            var scene = presenter.Stage.Current;
            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.Attacker, Is.EqualTo(KineticSubject.ForHero(blueHeroId)));
            Assert.That(scene.Defender, Is.EqualTo(KineticSubject.ForHero(redHeroId)));
            Assert.That(scene.AttackCard, Is.Not.Null,
                "Both faces are CAPTURED into the scene - the attacker's card...");
            Assert.That(scene.DefenseCard, Is.Null.Or.Not.Null,
                "...and the defense card, which is legitimately absent on a declined defense.");

            // One real frame, nothing ticked here: the driver is what advances the framing.
            var weightBefore = spotlight.Model.FramingWeight;
            yield return null;
            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "The driver isolates its tick; a failure here means the spotlight already fell back.");
            Assert.That(spotlight.Model.FramingWeight, Is.Not.EqualTo(weightBefore),
                "The framing advanced across one real frame without this case ticking it, which is " +
                "the whole of what 'plays on the real HUD' means.");

            // FRAME-SIZED STEPS, and this is the trap worker-spotlight recorded and I walked into
            // anyway on the first run: `AttackSpotlightStage.Advance` clamps ANY single delta to
            // 0.25s, so that a hitched frame cannot skip a 0.45s framing past its middle. One
            // Tick(EnterSeconds) therefore advances 0.25s, not 0.45s, and the stage is still
            // Entering - a case that hands it one big delta is testing something other than what it
            // reads as testing.
            TickBy(presenter, AttackSpotlightTiming.EnterSeconds + 0.05f);
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "The camera arrives and the shot holds for the viewer.");
            Assert.That(spotlight.Model.IsContinueArmed, Is.True, "CONTINUE is pressable.");
            Assert.That(spotlight.IsChromeHidden, Is.True,
                "and the HUD is out of the way, which is the reason this scene's hold valve is the " +
                "short one - see KineticStaging.ChromeHiddenHoldSeconds.");
            Assert.That(spotlight.IsDrivingCamera, Is.True, "The rig owns the board camera.");

            // The deferral's whole point, on the real surface: cues were held while the camera
            // travelled and are released HERE, framed, with both faces up.
            Assert.That(presenter.DefersCues, Is.False,
                "Held is deliberately not a deferring phase - the smack belongs at the END, framed.");

            // ---- CONTINUE gives the board back. ----
            presenter.Continue();
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Exiting));
            TickBy(presenter, AttackSpotlightTiming.ExitSeconds + 0.05f);
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));

            yield return null;
            Assert.That(spotlight.IsChromeHidden, Is.False,
                "Every panel the spotlight hid is back.");
            Assert.That(spotlight.IsDrivingCamera, Is.False,
                "and the board camera was handed back to the board, which reasserts its own frame.");
            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "Nothing in the pass threw across the whole journey.");
            Assert.That(Object.FindAnyObjectByType<KineticCueDriver>().FailureCount, Is.EqualTo(0),
                "and the kinetic driver rode the whole spotlight without a failure either, which is " +
                "what the deferral pairing has to be true for.");
        }

        /// <summary>
        /// Advances the spotlight in frame-sized steps. `AttackSpotlightStage.Advance` clamps any
        /// single delta to 0.25s, so a case that hands it one large delta silently advances less
        /// than it asked for.
        /// </summary>
        private static void TickBy(AttackSpotlightPresenter presenter, float seconds)
        {
            const float Frame = 1f / 60f;
            for (var elapsed = 0f; elapsed < seconds; elapsed += Frame)
            {
                presenter.Tick(Frame);
            }
        }

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;

        private static void Commit(
            ConfiguredMatchSceneRuntime runtime, TeamSide side, CardDefinitionId definitionId)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);

            var heroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        private static void AnswerAnyEndOfRoundPause(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                {
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId),
                        Is.True);
                }

                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                return;
            }

            if (runtime.PausedSystemTransitionOptions.Count > 0)
            {
                Assert.That(
                    runtime.AnswerPausedSystemTransition(runtime.PausedSystemTransitionOptions[0]),
                    Is.True);
                return;
            }

            Assert.That(runtime.ContinueEndOfRound(), Is.True);
        }

        private static ActionKind ActionKindOf(
            ConfiguredMatchSceneRuntime runtime, ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}

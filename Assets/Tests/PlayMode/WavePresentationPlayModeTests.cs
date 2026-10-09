using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
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

        [UnityTest]
        public IEnumerator AfterAttackQuestionAppearsOnlyAfterTheFramedAttackStarts()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);

            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));

            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minion = Object.FindObjectsByType<StandardMinionView>().Single(value =>
                value.Occupancy != null && value.Occupancy.MinionId.Equals(targetId));
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), hud,
                minion.transform.position), Is.True);

            // The command refresh is synchronous. Assert before yielding a real frame: on a loaded
            // batch runner one frame can exceed the entire enter-and-beat duration, which would
            // correctly release the question before this test had inspected the ordering boundary.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Shock's after-Attack hero chooser is authoritatively standing.");
            var attackRow = runtime.ReadModel.MatchLog.LastOrDefault(value =>
                value.Kind == GuardsDigital.Core.Diagnostics.MatchLogEntryKind.Attack);
            Assert.That(attackRow, Is.Not.Null,
                "The resolved-attack boundary must already have published the Attack row. Attack facts: " +
                string.Join(", ", runtime.DiagnosticEvents
                    .Where(value => value.EventType.Contains("Attack"))
                    .Select(value => value.EventType)) + ". Director rows: " +
                string.Join(", ", runtime.Projection.MatchLog.Select(value =>
                    value.Sequence + ":" + value.Kind)) + ".");
            Assert.That(runtime.SpotlightPresenter.DefersCues, Is.True,
                "The resolved Attack row " + attackRow.Sequence + " has claimed its local " +
                "presentation before the question draws. Stage=" +
                runtime.SpotlightPresenter.Stage.Phase + ", observed=" +
                runtime.SpotlightPresenter.ObservedSequence + ", skipped=" +
                runtime.SpotlightPresenter.SkippedWindowCount + ".");
            Assert.That(runtime.DecisionWindowView.Model, Is.Null,
                "The after-Attack question does not flash before the framed Attack.");

            if (runtime.SpotlightPresenter.Stage.Phase == AttackSpotlightPhase.Requested)
            {
                runtime.KineticView.Tick(0f);
            }
            TickBy(runtime.SpotlightPresenter,
                AttackSpotlightTiming.EnterSeconds +
                AttackSpotlightTiming.PostZoomBeatSeconds + 0.05f);

            Assert.That(runtime.SpotlightPresenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held));
            Assert.That(runtime.SpotlightPresenter.DefersCues, Is.False);
            Assert.That(runtime.DecisionWindowView.Model, Is.Null,
                "Releasing the queue is not enough: earlier scenes may still precede the Attack.");

            var lunge = KineticSubject.ForHero(redHeroId);
            for (var step = 0; step < 64 &&
                !runtime.KineticView.Playback.LungeTargetFor(lunge).IsDefined; step++)
            {
                runtime.KineticView.Tick(0.25f);
                if (!runtime.KineticView.Playback.LungeTargetFor(lunge).IsDefined)
                {
                    Assert.That(runtime.DecisionWindowView.Model, Is.Null,
                        "An earlier queued scene must not release the after-Attack question.");
                }
            }

            Assert.That(runtime.KineticView.Playback.LungeTargetFor(lunge).IsDefined,
                Is.True,
                "The Attack's framed smack reaches playback.");
            Assert.That(runtime.DecisionWindowView.Model, Is.Not.Null,
                "The untouched authoritative after-Attack question is released after the Attack starts.");
            Assert.That(runtime.DecisionWindowView.Model.Family,
                Is.EqualTo(DecisionWindowFamily.PieceOrSpace),
                "Shock's first rider question is still the real board-target chooser.");
            yield return UiPlayer.CaptureScreen(
                "tmp/943-captures/attack-before-after-attack-question.png");
        }

        [UnityTest]
        public IEnumerator LastMinionAttack_DiesAtItsOldSpaceBeforeItsSameIdentityRespawns()
        {
            yield return LoadEngagedScene();
            var initial = Root().Runtime;
            var attackerId = HeroOf(initial, TeamSide.Red);
            var attackerAt = initial.Heroes.Single(hero => hero.HeroId.Equals(attackerId)).Coordinate;
            var target = initial.Projection.Minions
                .Where(minion => minion.TeamSide == TeamSide.Blue &&
                    minion.MinionType != StandardMinionType.Heavy)
                .OrderBy(minion => HexDistanceQuery.Calculate(attackerAt, minion.Coordinate))
                .ThenBy(minion => minion.MinionId.Value)
                .First();
            Assert.That(HexDistanceQuery.Calculate(attackerAt, target.Coordinate), Is.LessThanOrEqualTo(4),
                "The staged last minion starts inside Shock's printed Range 4.");

            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(attackerId, hand: new[] { WaspCardContent.Shock })
                },
                removedMinions: initial.Projection.Minions
                    .Where(minion => minion.TeamSide == target.TeamSide &&
                        !minion.MinionId.Equals(target.MinionId))
                    .Select(minion => minion.MinionId));
            yield return LoadEngagedScene();

            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var defenderHeroId = HeroOf(runtime, TeamSide.Blue);
            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(attackerId));

            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds,
                Does.Contain(target.MinionId));
            var targetView = Object.FindObjectsByType<StandardMinionView>().Single(value =>
                value.Occupancy != null && value.Occupancy.MinionId.Equals(target.MinionId));
            var oldCoordinate = targetView.Occupancy.Coordinate;
            var oldPosition = targetView.transform.position;
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), hud,
                targetView.transform.position), Is.True);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Shock finishes its printed after-attack rider before the requested Lane Push.");
            var defenderHero = Object.FindObjectsByType<PrimitiveHeroView>().Single(value =>
                value.Occupancy.HeroId.Equals(defenderHeroId));
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), hud,
                defenderHero.transform.position), Is.True,
                "The enemy hero in Shock's disk answers on the board.");
            var discard = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(runtime.SubmitProjectedCommand(discard.Command), Is.True,
                "The chosen hero discards one projected hand card, completing Shock's rider.");
            Assert.That(runtime.PendingEffectChoice, Is.Null);

            var respawned = runtime.BoardView.Model.Minions.Single(value =>
                value.MinionId.Equals(target.MinionId));
            Assert.That(respawned.Coordinate, Is.Not.EqualTo(oldCoordinate),
                "The attack reproduced the reported edge: Lane Push reused the killed miniature's " +
                "identity at a spawn in the new Battle Zone inside the same projection.");
            Assert.That(targetView.gameObject.activeSelf, Is.False,
                "The real respawn must stay hidden while the old-space death is pending.");
            var ghost = runtime.BoardView.MinionDefeatGhostFor(targetView);
            Assert.That(ghost, Is.Not.Null);
            Assert.That(ghost.transform.position, Is.EqualTo(oldPosition),
                "The visible target remains where the player actually struck it.");

            if (runtime.SpotlightPresenter.Stage.Phase == AttackSpotlightPhase.Requested)
            {
                runtime.KineticView.Tick(0f);
            }

            TickBy(runtime.SpotlightPresenter,
                AttackSpotlightTiming.EnterSeconds +
                AttackSpotlightTiming.PostZoomBeatSeconds + 0.05f);
            var attacker = KineticSubject.ForHero(attackerId);
            for (var step = 0; step < 64 &&
                !runtime.KineticView.Playback.LungeTargetFor(attacker).IsDefined; step++)
            {
                runtime.KineticView.Tick(0.25f);
            }

            Assert.That(runtime.KineticView.Playback.LungeTargetFor(attacker),
                Is.EqualTo(KineticSubject.ForMinion(target.MinionId)),
                "The framed strike still names the killed minion.");
            Assert.That(ghost.transform.position, Is.EqualTo(oldPosition),
                "At the strike itself the minion has not jumped to the next zone.");
            Assert.That(targetView.gameObject.activeSelf, Is.False);
            yield return UiPlayer.CaptureGameView("w018", "last-minion-strike-old-space");

            const float PresentationFrame = 1f / 60f;
            for (var step = 0; step < 120 &&
                runtime.BoardView.MinionDefeatGhostFor(targetView) != null; step++)
            {
                runtime.KineticView.Tick(PresentationFrame);
                runtime.BoardView.AdvanceDefeatFades(PresentationFrame);
            }

            Assert.That(runtime.BoardView.MinionDefeatGhostFor(targetView), Is.Null,
                "The old-space death releases after the strike within two seconds of presentation " +
                "time; a surviving ghost would mean the attack gate never opened.");
            Assert.That(targetView.gameObject.activeSelf, Is.True);
            Assert.That(targetView.Occupancy.Coordinate, Is.EqualTo(respawned.Coordinate),
                "Only then does the reused miniature appear at its new Battle-Zone spawn.");
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

            // THE 2026-09-08 CAMERA-TIMING DIRECTIVE ("zoom in, a small pause, then smack"),
            // milestone 8.35z56: the camera has arrived, but the beat has not passed yet, so
            // the smack must not land on the very tick the zoom does.
            Assert.That(presenter.DefersCues, Is.True,
                "Held has just begun - the post-zoom beat has not elapsed, so the smack still waits.");

            TickBy(presenter, AttackSpotlightTiming.PostZoomBeatSeconds + 0.05f);
            Assert.That(presenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "Still framed - the beat is far short of MaximumHeldSeconds.");

            // The deferral's whole point, on the real surface: cues were held while the camera
            // travelled AND for one beat after it arrived, and are released HERE, framed, with both
            // faces up.
            Assert.That(presenter.DefersCues, Is.False,
                "One beat after the camera settles, Held stops deferring - the smack belongs at the " +
                "END, framed.");

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

        [UnityTest]
        public IEnumerator AttackKeepsTheAttackerVisibleWhenTheDefenderIsDefeated() =>
            AttackKeepsTheAttackerVisible(defeatDefender: true);

        [UnityTest]
        public IEnumerator AttackKeepsTheAttackerVisibleWhenTheDefenderSurvives() =>
            AttackKeepsTheAttackerVisible(defeatDefender: false);

        private static IEnumerator AttackKeepsTheAttackerVisible(bool defeatDefender)
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var red = HeroOf(runtime, TeamSide.Red);
            var blue = HeroOf(runtime, TeamSide.Blue);
            // Stage adjacency through legal filler decisions. Preserve Red's Shock in hand so
            // the same attack can be answered with a sufficient printed Defense or declined.
            Commit(runtime, TeamSide.Red, WaspCardContent.LiftUp);
            Commit(runtime, TeamSide.Blue, WaspCardContent.Shock);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(red));
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(red)).Coordinate;
            var step = runtime.PendingMovementChoice.LegalDestinations.First(value =>
                HexDistanceQuery.Calculate(value.Destination, redAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null) Assert.That(runtime.SubmitEndTurn(), Is.True);
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40));
                AnswerAnyEndOfRoundPause(runtime);
            }
            Commit(runtime, TeamSide.Blue, WaspCardContent.MagneticDagger);
            Commit(runtime, TeamSide.Red, WaspCardContent.StaticBarrier);
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blue));
            yield return null;
            yield return null;
            var snapshot = runtime.CaptureSnapshot();
            var attacker = Object.FindObjectsByType<PrimitiveHeroView>().Single(value => value.Occupancy.HeroId.Equals(blue));
            var defender = Object.FindObjectsByType<PrimitiveHeroView>().Single(value => value.Occupancy.HeroId.Equals(red));
            var scale = attacker.transform.localScale;
            var control = UiPlayer.Descendants(hud.RootElement).Single(element =>
                element.name == "gd-printed-action-region" &&
                element.userData is PrintedActionRegionViewModel region && region.ActionKind == ActionKind.Attack);
            UiPlayer.Click(control);
            yield return null;
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                hud, defender.transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            if (defeatDefender)
            {
                // Milestone 8.46c: the decline is the decision window's own corner answer now.
                UiPlayer.ClickDecisionCorner(runtime, hud);
            }
            else
            {
                var defense = UiPlayer.NamedElementWithText(hud, "gd-defense-card", "SHOCK");
                UiPlayer.EnsureVisible(defense);
                yield return null;
                UiPlayer.Click(defense);
            }
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(red)), Is.EqualTo(!defeatDefender),
                string.Join("\n", runtime.DiagnosticEvents.Reverse().Take(18).Select(entry => entry.Detail)));
            var sawFraming = false;
            var sawStrike = false;
            var subject = KineticSubject.ForHero(blue);
            var deadline = Time.realtimeSinceStartup + 15f;
            do
            {
                yield return null;
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Attack presentation must settle.");
                Assert.That(attacker.gameObject.activeInHierarchy, Is.True);
                Assert.That(attacker.transform.localScale.x, Is.GreaterThanOrEqualTo(scale.x * 0.99f));
                Assert.That(runtime.KineticView.Playback.MotionFor(subject).Scale, Is.EqualTo(1f));
                sawFraming |= runtime.SpotlightView.IsDrivingCamera;
                if (runtime.KineticView.Playback.LungeTargetFor(subject).IsDefined && !sawStrike)
                {
                    sawStrike = true;
                    Assert.That(runtime.SpotlightPresenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held));
                    if (defeatDefender)
                    {
                        Assert.That(runtime.BoardView.HeroDefeatGhostFor(defender), Is.Not.Null,
                            "The defeated hero's visual echo must survive the zoom until the strike.");
                    }
                    var camera = runtime.SpotlightView.CameraSource();
                    foreach (var token in new[] { attacker, defender })
                    {
                        var point = MatchHudPanelScaling.ScreenToPanelPoint(
                            camera.WorldToScreenPoint(token.transform.position), Screen.width, Screen.height);
                        var edge = MatchHudPanelScaling.ScreenToPanelPoint(
                            camera.WorldToScreenPoint(token.transform.position + camera.transform.right * token.SelectionRadius),
                            Screen.width, Screen.height);
                        var radius = Vector2.Distance(point, edge);
                        foreach (var face in UiPlayer.Descendants(hud.RootElement).Where(element =>
                                     (element.name == "gd-spotlight-attacker-card" ||
                                      element.name == "gd-spotlight-defender-card") && UiPlayer.IsShown(element)))
                        {
                            var rect = face.worldBound;
                            var closest = new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax),
                                Mathf.Clamp(point.y, rect.yMin, rect.yMax));
                            Assert.That(Vector2.Distance(point, closest), Is.GreaterThanOrEqualTo(radius - 1f),
                                "The attack's cards must not cover either zoomed token.");
                        }
                    }
                    yield return UiPlayer.CaptureGameView("8-36f2", defeatDefender ? "defeat-strike" : "surviving-strike");
                }
            } while (runtime.SpotlightPresenter.Stage.Phase != AttackSpotlightPhase.None ||
                     !runtime.KineticView.Playback.IsIdle);
            Assert.That(sawFraming, Is.True);
            Assert.That(sawStrike, Is.True, "The real attack must zoom, then strike.");
            yield return UiPlayer.CaptureGameView("8-36f2", defeatDefender ? "defeat-settled" : "surviving-settled");
            Assert.That(runtime.Undo(), Is.True);
            yield return null;
            Assert.That(runtime.SpotlightPresenter.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
            Assert.That(runtime.RestoreSnapshot(snapshot), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.KineticView.Playback.IsIdle, Is.True);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(red)), Is.True);
            Assert.That(attacker.transform.localScale.x, Is.EqualTo(scale.x).Within(0.001f));
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

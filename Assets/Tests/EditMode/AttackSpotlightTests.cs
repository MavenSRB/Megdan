using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z12. The NINETEENTH AMENDMENT's attack spotlight at the presenter and
    /// scene-model tier: what opens one, what it carries, what arms the continue, what a second attack
    /// does to the one on screen, and what an undo does to all of it.
    ///
    /// <para>Every case here runs with no Unity runtime, no scene and no composed match, because the
    /// lifecycle is a state machine over log rows and the log is the only input it has. What
    /// deliberately is NOT here is anything a camera, a panel or a card face has to be alive to answer -
    /// those are recorded as Unity-tier obligations in the milestone note rather than faked into a
    /// green result here.</para>
    /// </summary>
    public sealed class AttackSpotlightTests
    {
        private static readonly HeroId Attacker = new HeroId("spotlight-attacker");
        private static readonly HeroId Defender = new HeroId("spotlight-defender");
        private static readonly MinionId Minion = new MinionId("spotlight-minion");
        private static readonly CardInstanceId AttackCard = new CardInstanceId("attack-card");
        private static readonly CardInstanceId DefenseCard = new CardInstanceId("defense-card");
        private static readonly CardInstanceId OtherDefenseCard = new CardInstanceId("other-defense-card");

        // ---- The stage: the lifecycle, with no log and no projection at all.

        [Test]
        public void AnIdleStage_HoldsNothingAndShowsNothing()
        {
            var stage = new AttackSpotlightStage();

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
            Assert.That(stage.HoldsStage, Is.False);
            Assert.That(stage.IsRequestingStage, Is.False);
            Assert.That(stage.IsContinueArmed, Is.False);
            Assert.That(stage.FramingWeight, Is.EqualTo(0f));
            Assert.That(stage.Current, Is.Null);
            Assert.That(() => stage.Advance(1f), Throws.Nothing);
            Assert.That(() => stage.Interrupt(ConductedSceneInterruption.Shutdown), Throws.Nothing);
        }

        /// <summary>
        /// The whole user-stated sequence in one case: lose the UI, zoom in, show the cards, show the
        /// smack, and have a continue button.
        /// </summary>
        [Test]
        public void AnOfferedScene_Enters_Arms_AndLeavesOnlyWhenTheViewerPresses()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(7));

            Assert.That(stage.HoldsStage, Is.True, "The stage is taken the moment a scene is offered.");
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Entering));
            Assert.That(stage.IsContinueArmed, Is.False,
                "CONTINUE is not pressable while the camera is still travelling.");
            Assert.That(stage.FramingWeight, Is.EqualTo(0f).Within(0.0001f));

            AdvanceBy(stage, AttackSpotlightTiming.EnterSeconds + 0.02f);

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held));
            Assert.That(stage.IsContinueArmed, Is.True);
            Assert.That(stage.FramingWeight, Is.EqualTo(1f).Within(0.0001f));

            // The hold lasts exactly as long as the viewer wants it to, short of the safety valve.
            AdvanceBy(stage, AttackSpotlightTiming.MaximumHeldSeconds - 1f);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "Nothing but the viewer's press ends the hold.");

            Assert.That(stage.Continue(), Is.True);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Exiting));
            Assert.That(stage.HoldsStage, Is.True, "The stage is still ours while the camera returns.");

            AdvanceBy(stage, AttackSpotlightTiming.ExitSeconds + 0.02f);

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
            Assert.That(stage.HoldsStage, Is.False);
            Assert.That(stage.FramingWeight, Is.EqualTo(0f));
            Assert.That(stage.Current, Is.Null);
        }

        /// <summary>
        /// The HUD is hidden while a spotlight is up, so a viewer who walks away must not come back to
        /// a client they cannot play. The authority never waited for them.
        /// </summary>
        [Test]
        public void AHeldSpotlight_ReleasesItselfRatherThanTrapTheViewerBehindAHiddenHud()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(1));
            AdvanceBy(stage, AttackSpotlightTiming.EnterSeconds + 0.02f);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held));

            AdvanceBy(stage, AttackSpotlightTiming.MaximumHeldSeconds + 0.05f);

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Exiting));
            AdvanceBy(stage, AttackSpotlightTiming.ExitSeconds + 0.02f);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
        }

        [Test]
        public void ASecondAttack_WaitsBehindTheOneOnScreenAndThenPlaysInOrder()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(4));
            stage.Offer(Scene(5));
            stage.Offer(Scene(6));

            Assert.That(stage.Current.Sequence, Is.EqualTo(4));
            Assert.That(stage.QueuedSceneCount, Is.EqualTo(2));
            Assert.That(stage.DroppedSceneCount, Is.Zero);

            PlayOneThrough(stage);
            Assert.That(stage.Current.Sequence, Is.EqualTo(5),
                "The queue plays in the order the attacks happened.");
            PlayOneThrough(stage);
            Assert.That(stage.Current.Sequence, Is.EqualTo(6));
            PlayOneThrough(stage);
            Assert.That(stage.Current, Is.Null);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
        }

        [Test]
        public void AQueueBeyondItsBound_DropsTheOldestAndSaysHowMany()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(1));
            for (var sequence = 2; sequence <= 2 + AttackSpotlightTiming.MaxQueuedScenes; sequence++)
            {
                stage.Offer(Scene(sequence));
            }

            Assert.That(stage.QueuedSceneCount, Is.EqualTo(AttackSpotlightTiming.MaxQueuedScenes));
            Assert.That(stage.DroppedSceneCount, Is.EqualTo(1));

            PlayOneThrough(stage);
            Assert.That(stage.Current.Sequence, Is.EqualTo(3),
                "Sequence 2 was the oldest waiting scene and is the one that went.");
        }

        [Test]
        public void TheSameAttackOfferedTwice_IsWatchedOnce()
        {
            var stage = new AttackSpotlightStage();
            Assert.That(stage.Offer(Scene(9)), Is.True);
            Assert.That(stage.Offer(Scene(9)), Is.False, "The scene on screen is not re-offered.");
            stage.Offer(Scene(10));
            Assert.That(stage.Offer(Scene(10)), Is.False, "Nor is a scene already in the queue.");
            Assert.That(stage.QueuedSceneCount, Is.EqualTo(1));
        }

        /// <summary>
        /// An undo, a rewind, a restore. The spotlight may be describing an attack that no longer
        /// happened, so it goes at once and with no easing - the surface must be authoritative on the
        /// very next frame rather than 350ms later.
        /// </summary>
        [Test]
        public void AnInterrupt_SnapsWithNoEasingAndTakesTheQueueWithIt()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(1));
            stage.Offer(Scene(2));
            AdvanceBy(stage, AttackSpotlightTiming.EnterSeconds + 0.02f);
            Assert.That(stage.FramingWeight, Is.EqualTo(1f).Within(0.0001f));

            stage.Interrupt(ConductedSceneInterruption.ProjectionRewound);

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.None));
            Assert.That(stage.FramingWeight, Is.EqualTo(0f),
                "There is no exit animation out of an interruption: the surface is authoritative now.");
            Assert.That(stage.HoldsStage, Is.False);
            Assert.That(stage.QueuedSceneCount, Is.Zero,
                "A queued scene derived from the same rewound log cannot be shown either.");
            Assert.That(stage.LastInterruption,
                Is.EqualTo(ConductedSceneInterruption.ProjectionRewound));
        }

        /// <summary>
        /// The conductor contract, from the conductor's side: a scene that is not granted the stage
        /// changes nothing a player can see, and one grant starts it.
        /// </summary>
        [Test]
        public void WithAConductorInCharge_ASceneWaitsToBeGrantedAndHidesNothingWhileItWaits()
        {
            var stage = new AttackSpotlightStage { AutoGrantsStage = false };
            stage.Offer(Scene(3));

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Requested));
            Assert.That(stage.IsRequestingStage, Is.True);
            Assert.That(stage.HoldsStage, Is.False,
                "A scene waiting its turn has taken nothing from the surface.");
            Assert.That(stage.FramingWeight, Is.EqualTo(0f));
            AdvanceBy(stage, 5f);
            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Requested),
                "Waiting does not time out - the conductor decides, and it has not yet.");

            stage.GrantStage();

            Assert.That(stage.Phase, Is.EqualTo(AttackSpotlightPhase.Entering));
            Assert.That(stage.IsRequestingStage, Is.False);
            Assert.That(stage.HoldsStage, Is.True);
        }

        [Test]
        public void TheFramingWeight_IsEasedAndRisesThenFalls()
        {
            var stage = new AttackSpotlightStage();
            stage.Offer(Scene(1));

            var previous = -1f;
            for (var step = 0; step < 9; step++)
            {
                var weight = stage.FramingWeight;
                Assert.That(weight, Is.GreaterThanOrEqualTo(previous),
                    "The framing never goes backwards while the camera travels in.");
                Assert.That(weight, Is.InRange(0f, 1f));
                previous = weight;
                stage.Advance(AttackSpotlightTiming.EnterSeconds / 8f);
            }

            Assert.That(stage.FramingWeight, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(AttackSpotlightTiming.Ease(0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(AttackSpotlightTiming.Ease(0.25f), Is.LessThan(0.25f),
                "Eased in, so the move starts gently rather than at full speed.");
            Assert.That(AttackSpotlightTiming.Ease(0.75f), Is.GreaterThan(0.75f),
                "and eased out, so it settles rather than stopping dead.");

            stage.Continue();
            previous = 2f;
            for (var step = 0; step < 5; step++)
            {
                var weight = stage.FramingWeight;
                Assert.That(weight, Is.LessThanOrEqualTo(previous));
                previous = weight;
                stage.Advance(AttackSpotlightTiming.ExitSeconds / 4f);
            }
        }

        /// <summary>The amendment's own stated band, kept honest against the constant.</summary>
        [Test]
        public void TheTilt_StaysInsideTheAmendmentsOwnBand()
        {
            Assert.That(AttackSpotlightTiming.TiltDegrees,
                Is.InRange(
                    AttackSpotlightTiming.MinimumTiltDegrees,
                    AttackSpotlightTiming.MaximumTiltDegrees),
                "The NINETEENTH AMENDMENT asked for a modest tilt of about 10-20 degrees.");
        }

        // ---- The presenter: what a log row becomes.

        [Test]
        public void TheFirstProjectionEverSeen_PrimesTheWatermarkAndOpensNothing()
        {
            var view = new CapturingSpotlightView();
            var client = new SpotlightClient(
                new SpotlightReadModel(new[] { Attack(12, Attacker, Defender, AttackCard) }));
            var presenter = new AttackSpotlightPresenter(client, view);

            presenter.Refresh();

            Assert.That(presenter.ObservedSequence, Is.EqualTo(12));
            Assert.That(view.Model.IsActive, Is.False,
                "A client joining a match in progress must not watch every attack it missed.");
            Assert.That(presenter.HoldsStage, Is.False);
        }

        [Test]
        public void AResolvedAttack_OpensASpotlightCarryingBothPublicFaces()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            Publish(
                client,
                presenter,
                new[]
                {
                    Round(1),
                    Defense(4, Defender, Attacker, DefenseCard),
                    Attack(5, Attacker, Defender, AttackCard,
                        MatchLogOutcome.AttackSuccessfullyDefended)
                },
                new[] { Card(AttackCard, Attacker), Card(DefenseCard, Defender) });

            var scene = view.Model.Scene;
            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.Sequence, Is.EqualTo(5), "The scene keeps the Attack row's own identity.");
            Assert.That(scene.Attacker.HeroId, Is.EqualTo(Attacker));
            Assert.That(scene.Defender.HeroId, Is.EqualTo(Defender));
            Assert.That(scene.HasAttackCard, Is.True,
                "The attacker's revealed card is face-up in the turn area and public to every seat.");
            Assert.That(scene.AttackCard.CardInstanceId, Is.EqualTo(AttackCard));
            Assert.That(scene.HasDefenseCard, Is.True,
                "The spent defense card is face-up in a discard pile and public to every seat.");
            Assert.That(scene.DefenseCard.CardInstanceId, Is.EqualTo(DefenseCard));
            Assert.That(scene.WasDefended, Is.True);
            Assert.That(view.Model.IsActive, Is.True);
            Assert.That(presenter.DefersCues, Is.True,
                "The smack waits until the camera has arrived.");
        }

        [Test]
        public void AnAttackNobodyDefended_CarriesTheAttackFaceAndNoOther()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            Publish(
                client,
                presenter,
                new[]
                {
                    Round(1),
                    Defense(4, Defender, Attacker, default, MatchLogOutcome.DefenseDeclined),
                    Attack(5, Attacker, Defender, AttackCard)
                },
                new[] { Card(AttackCard, Attacker) });

            Assert.That(view.Model.Scene.HasAttackCard, Is.True);
            Assert.That(view.Model.Scene.HasDefenseCard, Is.False,
                "A declined defense spent no card, so there is no second face to show.");
            Assert.That(view.Model.Scene.WasDefended, Is.False);
        }

        /// <summary>
        /// A match-log row carries no resolution id, so the defense is paired by walking backwards and
        /// stopping at the previous Attack. Two attacks in one turn is exactly the shape that pairing
        /// has to get right.
        /// </summary>
        [Test]
        public void ADefenseSpentAgainstAnEarlierAttackInTheSameTurn_IsNeverBorrowed()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            Publish(
                client,
                presenter,
                new[]
                {
                    Round(1),
                    Defense(4, Defender, Attacker, OtherDefenseCard),
                    Attack(5, Attacker, Defender, AttackCard,
                        MatchLogOutcome.AttackSuccessfullyDefended),
                    Attack(6, Attacker, Defender, AttackCard)
                },
                new[]
                {
                    Card(AttackCard, Attacker), Card(DefenseCard, Defender),
                    Card(OtherDefenseCard, Defender)
                });

            var first = view.Model.Scene;
            Assert.That(first.Sequence, Is.EqualTo(5));
            Assert.That(first.DefenseCard.CardInstanceId, Is.EqualTo(OtherDefenseCard));

            // The second attack in the same turn had no defense of its own.
            var stage = presenter.Stage;
            Assert.That(stage.QueuedSceneCount, Is.EqualTo(1));
            PlayOneThrough(stage);
            Assert.That(stage.Current.Sequence, Is.EqualTo(6));
            Assert.That(stage.Current.HasDefenseCard, Is.False,
                "Walking back stops at the previous Attack row, so the earlier defense stays there.");
        }

        [Test]
        public void AnAttackOnAMinion_FramesTheMinionAndLooksForNoDefenseCard()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            Publish(
                client,
                presenter,
                new[]
                {
                    Round(1),
                    Defense(4, Defender, Attacker, DefenseCard),
                    new MatchLogEntry(
                        5, MatchLogEntryKind.Attack, 1, 1,
                        subjectHeroId: Attacker,
                        subjectMinionId: Minion,
                        subjectCardInstanceId: AttackCard,
                        outcome: MatchLogOutcome.AttackSuccessful)
                },
                new[] { Card(AttackCard, Attacker), Card(DefenseCard, Defender) });

            var scene = view.Model.Scene;
            Assert.That(scene.Defender.Kind, Is.EqualTo(KineticSubjectKind.Minion));
            Assert.That(scene.Defender.MinionId, Is.EqualTo(Minion));
            Assert.That(scene.HasDefenseCard, Is.False,
                "Nothing but a hero answers a defense, so an attack on a minion looks for no card.");
        }

        /// <summary>
        /// A watermark alone does not cover the client whose FIRST sight was an empty log and whose
        /// second is a match in progress - a seat admission, a resync, a snapshot restore. That
        /// client's watermark is honestly zero and every historical attack is honestly new, so
        /// without this rule it would be walked through a reel of spotlights with a button between
        /// each. Two attacks in one authoritative step is a repeated attack and still plays.
        /// </summary>
        [Test]
        public void AWindowOfAttacksArrivingAtOnce_PrimesRatherThanQueueingAReel()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            var rows = new List<MatchLogEntry> { Round(1) };
            for (var sequence = 2; sequence < 2 + AttackSpotlightTiming.MaxQueuedScenes + 2; sequence++)
            {
                rows.Add(Attack(sequence, Attacker, Defender, AttackCard));
            }

            Publish(client, presenter, rows.ToArray(), new[] { Card(AttackCard, Attacker) });

            Assert.That(view.Model.IsActive, Is.False);
            Assert.That(presenter.SkippedWindowCount, Is.EqualTo(1));
            Assert.That(presenter.ObservedSequence, Is.EqualTo(rows[rows.Count - 1].Sequence),
                "It primes to the window's end, so the next real attack still opens a spotlight.");

            Publish(
                client, presenter,
                rows.Concat(new[] { Attack(20, Attacker, Defender, AttackCard) }).ToArray(),
                new[] { Card(AttackCard, Attacker) });
            Assert.That(view.Model.Scene?.Sequence, Is.EqualTo(20));
        }

        [Test]
        public void TwoAttacksInOneStep_BothPlay_BecauseARepeatedAttackIsARealShape()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);

            Publish(
                client,
                presenter,
                new[]
                {
                    Round(1),
                    Attack(4, Attacker, Defender, AttackCard),
                    Attack(5, Attacker, Defender, AttackCard)
                },
                new[] { Card(AttackCard, Attacker) });

            Assert.That(view.Model.Scene.Sequence, Is.EqualTo(4));
            Assert.That(presenter.Stage.QueuedSceneCount, Is.EqualTo(1));
            Assert.That(presenter.SkippedWindowCount, Is.Zero);
        }

        /// <summary>
        /// The ranged fact end to end, against the real card the ranged pass was opened on: the row
        /// names a card, the card's definition prints a Ranged Attack, and the scene says so - with
        /// nothing added to the wire, because every step of that is already public.
        /// </summary>
        [Test]
        public void ASceneCarriesWhetherTheCardItNamesPrintsARangedAttack()
        {
            var shootout = SabinaCardContent.AllDefinitions
                .Single(definition => definition.Id.Equals(SabinaCardContent.Shootout));
            var melee = SabinaCardContent.AllDefinitions.First(definition =>
                definition.ActionDefinitions.All(
                    action => action.AttackRange != AttackRangeKind.Ranged));

            Assert.That(SceneFor(shootout).IsRangedAttack, Is.True,
                "Shootout prints Range 4 and the spotlight must be able to say so.");
            Assert.That(SceneFor(melee).IsRangedAttack, Is.False);
        }

        private static AttackSpotlightScene SceneFor(CardDefinition definition)
        {
            var view = new CapturingSpotlightView();
            var card = new CardInstance(
                AttackCard, definition.Reference, Attacker, CardStateFacts.InitialInHand());
            var client = new SpotlightClient(new SpotlightReadModel(new[] { Round(1) }));
            var presenter = new AttackSpotlightPresenter(client, view);
            presenter.Refresh();

            client.Projection = new SpotlightReadModel(
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) },
                new[] { card },
                new[] { definition });
            presenter.Refresh();
            return view.Model.Scene;
        }

        [Test]
        public void ALogThatWentBackwards_InterruptsWhateverIsOnScreenAndSnaps()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);
            Publish(
                client, presenter,
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) },
                new[] { Card(AttackCard, Attacker) });
            Assert.That(view.Model.IsActive, Is.True);

            Publish(client, presenter, new[] { Round(1) });

            Assert.That(view.Model.IsActive, Is.False);
            Assert.That(view.Model.SnapToAuthoritative, Is.True,
                "The surface is told to drop everything, not to ease out of an undone attack.");
            Assert.That(presenter.Stage.LastInterruption,
                Is.EqualTo(ConductedSceneInterruption.ProjectionRewound));
        }

        /// <summary>
        /// The reason the faces are captured rather than looked up: the viewer decides when to leave,
        /// so the match has moved on by the time they do.
        /// </summary>
        [Test]
        public void ASceneKeepsTheFacesItWasBuiltWith_EvenAfterTheCardsHaveMovedOn()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);
            var log = new[]
            {
                Round(1),
                Defense(4, Defender, Attacker, DefenseCard),
                Attack(5, Attacker, Defender, AttackCard,
                    MatchLogOutcome.AttackSuccessfullyDefended)
            };
            Publish(
                client, presenter, log,
                new[] { Card(AttackCard, Attacker), Card(DefenseCard, Defender) });
            var scene = view.Model.Scene;

            // The turn moves on: neither card is anything this viewer can name any more.
            Publish(client, presenter, log, Array.Empty<CardInstance>());

            Assert.That(view.Model.Scene, Is.SameAs(scene));
            Assert.That(view.Model.Scene.AttackCard.CardInstanceId, Is.EqualTo(AttackCard));
            Assert.That(view.Model.Scene.DefenseCard.CardInstanceId, Is.EqualTo(DefenseCard));
        }

        [Test]
        public void ALostProjection_TakesTheSpotlightDownRatherThanLeaveItStanding()
        {
            var view = new CapturingSpotlightView();
            var client = Primed(view, out var presenter);
            Publish(
                client, presenter,
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) },
                new[] { Card(AttackCard, Attacker) });
            Assert.That(view.Model.IsActive, Is.True);

            client.Projection = null;
            presenter.Refresh();

            Assert.That(view.Model.IsActive, Is.False);
            Assert.That(presenter.Stage.LastInterruption,
                Is.EqualTo(ConductedSceneInterruption.Disconnected));
        }

        // ---- The pairing with the kinetic panel: the amendment's buffered cue queue.

        /// <summary>
        /// The user's stated order, refined by the 2026-09-08 camera-timing directive, is "zoom in, a
        /// small pause, then smack". This is that order as a property: the lunge does not play while
        /// the camera travels, nothing is lost while it waits, it does NOT play on the very tick the
        /// camera arrives either (milestone 8.35z56's fix for the directive - before it, this
        /// assertion is the one the live playtest's "they overlap" report reproduces), and it plays in
        /// full one <see cref="AttackSpotlightTiming.PostZoomBeatSeconds"/> beat after the camera
        /// settles.
        /// </summary>
        [Test]
        public void TheSmack_WaitsForTheCameraAndTheBeatAndThenPlaysInFull()
        {
            var spotlightView = new CapturingSpotlightView();
            var kineticView = new CapturingKineticView();
            var client = new SpotlightClient(new SpotlightReadModel(new[] { Round(1) }));
            var spotlight = new AttackSpotlightPresenter(client, spotlightView);
            var kinetics = new KineticCuePresenter(client, kineticView);
            kinetics.DeferWhile(() => spotlight.DefersCues);
            spotlight.CuesReleased += () => kinetics.Resume();
            spotlight.Refresh();
            kinetics.Refresh();

            client.Projection = new SpotlightReadModel(
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) },
                new[] { Card(AttackCard, Attacker) });
            // The composition's own order: the spotlight sees the projection first, so its pacing
            // decides whether the kinetic panel below it plays this row or holds it.
            spotlight.Refresh();
            kinetics.Refresh();

            Assert.That(spotlight.DefersCues, Is.True);
            Assert.That(kinetics.IsDeferred, Is.True);
            Assert.That(kineticView.Model.Cues, Is.Empty,
                "The lunge does not play half off-centre while the camera is still moving.");
            Assert.That(kinetics.ObservedSequence, Is.LessThan(5),
                "and the row it did not play is still unobserved, so nothing has been lost.");

            TickBy(spotlight, AttackSpotlightTiming.EnterSeconds + 0.02f);

            Assert.That(spotlight.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "The camera has arrived.");
            Assert.That(spotlight.DefersCues, Is.True,
                "THE DIRECTIVE'S FIX: the beat has not passed yet, so the smack must not land on the " +
                "very tick the zoom does - reproducing this as False here is the overlap the user saw.");
            Assert.That(kineticView.Model.Cues, Is.Empty,
                "Framed, but still one beat early - the lunge keeps waiting.");
            Assert.That(kinetics.ObservedSequence, Is.LessThan(5),
                "and the row is still unobserved, so the beat costs nothing either.");

            TickBy(spotlight, AttackSpotlightTiming.PostZoomBeatSeconds + 0.02f);

            Assert.That(spotlight.Stage.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "Still framed - the beat is far short of MaximumHeldSeconds, so nobody's screen locks up.");
            Assert.That(spotlight.DefersCues, Is.False);
            Assert.That(kineticView.Model.Cues.Select(cue => cue.Kind),
                Contains.Item(KineticCueKind.Lunge),
                "The smack lands with both faces up and the two pieces framed, one beat after landing.");
            Assert.That(kinetics.ObservedSequence, Is.EqualTo(5));
        }

        /// <summary>
        /// Milestone <c>8.35z56</c>. The ranged tracer used to gate on the bare HELD phase, so the beat
        /// that held the attacker's jab did NOT hold the dash crossing the ground between the pieces -
        /// on a ranged pair the tracer went first and the jab followed a beat later, which is two
        /// events where the directive asks for one. The fix gives the view model the elapsed time and
        /// one shared predicate; this is that gate as a property, at the two moments that matter.
        ///
        /// <para>It is asserted on the RENDERED view model rather than inside
        /// <c>AttackSpotlightUnityView</c>, which is the point of moving the decision there: the raw
        /// view is UI Toolkit plus camera math with no offline coverage, while the answer it now reads
        /// is a pure property this fixture can pin exactly.</para>
        /// </summary>
        [Test]
        public void TheRangedTracer_HoldsForTheSameBeatTheLungeDoes()
        {
            var spotlightView = new CapturingSpotlightView();
            var client = new SpotlightClient(new SpotlightReadModel(new[] { Round(1) }));
            var spotlight = new AttackSpotlightPresenter(client, spotlightView);
            spotlight.Refresh();

            client.Projection = new SpotlightReadModel(
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) },
                new[] { Card(AttackCard, Attacker) });
            spotlight.Refresh();

            Assert.That(spotlightView.Model.Phase, Is.EqualTo(AttackSpotlightPhase.Entering));
            Assert.That(spotlightView.Model.DefersOwnMotion, Is.True,
                "While the camera travels the tracer must not draw - it never did.");

            TickBy(spotlight, AttackSpotlightTiming.EnterSeconds + 0.02f);

            Assert.That(spotlightView.Model.Phase, Is.EqualTo(AttackSpotlightPhase.Held),
                "The camera has arrived.");
            Assert.That(spotlightView.Model.ElapsedInPhase,
                Is.LessThan(AttackSpotlightTiming.PostZoomBeatSeconds),
                "The view model carries the elapsed time the gate needs; without it the surface " +
                "cannot tell this moment from the one after the beat.");
            Assert.That(spotlightView.Model.DefersOwnMotion, Is.True,
                "THE FIX: on the tick the zoom lands, the tracer holds exactly as the lunge does. " +
                "Reproducing this as False is the asymmetry the beat opened.");
            Assert.That(spotlightView.Model.DefersOwnMotion, Is.EqualTo(spotlight.DefersCues),
                "One gate, two consumers - the surface's answer and the cue layer's are the same " +
                "call, not two readings that agree today.");

            TickBy(spotlight, AttackSpotlightTiming.PostZoomBeatSeconds + 0.02f);

            Assert.That(spotlightView.Model.DefersOwnMotion, Is.False,
                "The beat has passed: the dash crosses the ground with the jab, not before it.");
            Assert.That(spotlightView.Model.DefersOwnMotion, Is.EqualTo(spotlight.DefersCues),
                "and the two consumers still answer together.");
        }

        [Test]
        public void ACompositionWithNoSpotlight_PlaysItsCuesExactlyAsItAlwaysDid()
        {
            var kineticView = new CapturingKineticView();
            var client = new SpotlightClient(new SpotlightReadModel(new[] { Round(1) }));
            var kinetics = new KineticCuePresenter(client, kineticView);
            kinetics.Refresh();

            client.Projection = new SpotlightReadModel(
                new[] { Round(1), Attack(5, Attacker, Defender, AttackCard) });
            kinetics.Refresh();

            Assert.That(kinetics.IsDeferred, Is.False);
            Assert.That(kineticView.Model.Cues.Select(cue => cue.Kind),
                Contains.Item(KineticCueKind.Lunge));
        }

        // ---- Helpers.

        private static SpotlightClient Primed(
            CapturingSpotlightView view, out AttackSpotlightPresenter presenter)
        {
            var client = new SpotlightClient(new SpotlightReadModel(new[] { Round(1) }));
            presenter = new AttackSpotlightPresenter(client, view);
            presenter.Refresh();
            return client;
        }

        /// <summary>
        /// Publishes a NEW projection - the shape a real authoritative change has - and refreshes.
        /// </summary>
        private static void Publish(
            SpotlightClient client,
            AttackSpotlightPresenter presenter,
            MatchLogEntry[] log,
            CardInstance[] cards = null)
        {
            client.Projection = new SpotlightReadModel(log, cards);
            presenter.Refresh();
        }

        private static AttackSpotlightScene Scene(int sequence) =>
            new AttackSpotlightScene(
                sequence,
                KineticSubject.ForHero(Attacker),
                KineticSubject.ForHero(Defender),
                MatchLogOutcome.AttackSuccessful);

        private static MatchLogEntry Round(int sequence) =>
            new MatchLogEntry(sequence, MatchLogEntryKind.RoundStarted, 1, 0);

        private static MatchLogEntry Attack(
            int sequence,
            HeroId attacker,
            HeroId defender,
            CardInstanceId card,
            MatchLogOutcome outcome = MatchLogOutcome.AttackSuccessful) =>
            new MatchLogEntry(
                sequence, MatchLogEntryKind.Attack, 1, 1,
                subjectHeroId: attacker,
                targetHeroId: defender,
                subjectCardInstanceId: card,
                outcome: outcome);

        private static MatchLogEntry Defense(
            int sequence,
            HeroId defender,
            HeroId attacker,
            CardInstanceId card,
            MatchLogOutcome outcome = MatchLogOutcome.DefenseCardDiscarded) =>
            // The Defense row's subject is the hero who ANSWERED - see MatchLogProjection's own note
            // on why the defender is the subject there and the attacker the target.
            new MatchLogEntry(
                sequence, MatchLogEntryKind.Defense, 1, 1,
                subjectHeroId: defender,
                targetHeroId: attacker,
                subjectCardInstanceId: card,
                outcome: outcome);

        private static CardInstance Card(CardInstanceId id, HeroId owner) =>
            CardInstance.CreateInHand(
                id,
                new CardDefinitionReference(
                    new CardDefinitionId("spotlight-card"), "spotlight-rev-1"),
                owner);

        /// <summary>
        /// Frame-sized steps rather than one large delta, because <c>Advance</c> deliberately clamps a
        /// single delta so a hitched frame cannot skip a 450ms framing past its middle.
        /// </summary>
        private static void AdvanceBy(AttackSpotlightStage stage, float seconds)
        {
            const float frame = 1f / 60f;
            var remaining = seconds;
            while (remaining > 0f)
            {
                stage.Advance(Math.Min(frame, remaining));
                remaining -= frame;
            }
        }

        /// <summary>
        /// The presenter's clock, in frame-sized steps, for the same reason
        /// <see cref="AdvanceBy"/> takes them: a case that hands the stage one 0.47-second delta is
        /// not testing what it thinks it is, because the clamp turns it into 0.25.
        /// </summary>
        private static void TickBy(AttackSpotlightPresenter presenter, float seconds)
        {
            const float frame = 1f / 60f;
            var remaining = seconds;
            while (remaining > 0f)
            {
                presenter.Tick(Math.Min(frame, remaining));
                remaining -= frame;
            }
        }

        private static void PlayOneThrough(AttackSpotlightStage stage)
        {
            AdvanceBy(stage, AttackSpotlightTiming.EnterSeconds + 0.02f);
            stage.Continue();
            AdvanceBy(stage, AttackSpotlightTiming.ExitSeconds + 0.02f);
        }

        private sealed class CapturingSpotlightView : IAttackSpotlightView
        {
            public AttackSpotlightViewModel Model { get; private set; } =
                AttackSpotlightViewModel.Empty;

            public void Render(AttackSpotlightViewModel model) => Model = model;
        }

        private sealed class CapturingKineticView : IKineticCueView
        {
            public KineticCueViewModel Model { get; private set; } = KineticCueViewModel.Empty;

            public void Render(KineticCueViewModel model) => Model = model;
        }

        private sealed class SpotlightClient : IMatchClient
        {
            public SpotlightClient(IMatchReadModel projection) => Projection = projection;

            /// <summary>
            /// Settable, because a real projection is a NEW OBJECT on every authoritative change -
            /// both <c>MatchSessionProjection</c> and <c>SeatProjection</c> are rebuilt rather than
            /// mutated - and both presenters under test skip an observation whose projection is
            /// reference-equal to the last one. A double that mutated one instance in place would
            /// quietly test nothing at all, which is exactly what this fixture did until it was run.
            /// </summary>
            public IMatchReadModel Projection { get; set; }

            public bool IsDirector => true;

            public MatchClientResult Submit(DirectorCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitUndo() =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");
        }

        /// <summary>
        /// The narrowest projection that answers what a spotlight and a kinetic stream read: a log, a
        /// card list, and the empty collections everything else takes. It is a stub rather than a
        /// composed match on purpose - composing one reaches <c>BoardMapJsonLoader</c>'s
        /// <c>Resources.Load</c>, which is engine-native and would put every case in this fixture
        /// behind a Unity launch.
        /// </summary>
        private sealed class SpotlightReadModel : IMatchReadModel
        {
            private static long published;
            private readonly long version;

            public SpotlightReadModel(
                IReadOnlyList<MatchLogEntry> log,
                IReadOnlyList<CardInstance> cards = null,
                IReadOnlyList<CardDefinition> definitions = null)
            {
                LogRows = log ?? Array.Empty<MatchLogEntry>();
                CardList = cards ?? Array.Empty<CardInstance>();
                DefinitionList = definitions ?? Array.Empty<CardDefinition>();
                version = ++published;
            }

            public IReadOnlyList<MatchLogEntry> LogRows { get; }
            public IReadOnlyList<CardInstance> CardList { get; }
            public IReadOnlyList<CardDefinition> DefinitionList { get; }

            public long Version => version;
            public bool IsInputEnabled => true;
            public bool IsSeatUndoAvailable => false;
            public QuickGameSetupState Setup => null;
            public BoardMapDefinition SelectedMap => null;
            public MatchParticipants Participants => null;
            public IReadOnlyList<CardDefinition> Definitions => DefinitionList;
            public IReadOnlyList<HeroOccupancy> Heroes => Array.Empty<HeroOccupancy>();
            public IReadOnlyList<MinionOccupancy> Minions => Array.Empty<MinionOccupancy>();
            public IReadOnlyList<ObstacleOccupancy> Obstacles => Array.Empty<ObstacleOccupancy>();
            public IReadOnlyList<PlacedToken> PlacedTokens => Array.Empty<PlacedToken>();
            public IReadOnlyList<MarkerAttachment> HeldMarkers => Array.Empty<MarkerAttachment>();

            public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
                Array.Empty<MarkerAttachment>();

            public int RoundIndex => 1;
            public int TurnIndex => 1;
            public RoundResolutionPhase Phase => default;
            public TieBreakerState TieBreakerState => default;
            public IReadOnlyList<RoundTurnSlot> TurnSlots => Array.Empty<RoundTurnSlot>();

            public IReadOnlyList<CompletedRoundRecord> CompletedRounds =>
                Array.Empty<CompletedRoundRecord>();

            public EndOfRoundStage? CurrentEndOfRoundStage => null;

            public IReadOnlyList<MatchCommitmentProjection> Commitments =>
                Array.Empty<MatchCommitmentProjection>();

            public MatchCommitmentProjection GetCommitment(HeroId heroId) => default;
            public SelectedInitiativeActor SelectedActor => default;
            public bool IsTurnActive => true;

            public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters =>
                Array.Empty<KeyValuePair<TeamId, int>>();

            // Milestone 8.35z60. This stub answers a spotlight/kinetic-stream read, neither
            // of which reads the Wave pool; a fixed value keeps the "narrowest projection" shape.
            public int RemainingWaveCounters => 0;

            public IReadOnlyList<HeroProgressionProjection> HeroProgression =>
                Array.Empty<HeroProgressionProjection>();

            public IReadOnlyList<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry> Scoreboard =>
                Array.Empty<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry>();

            public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome => default;

            public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds =>
                Array.Empty<CardInstanceId>();

            public bool IsActiveEffectSource(CardInstanceId cardInstanceId) => false;
            public TeamAgreementProjection TeamAgreement => null;
            public IReadOnlyList<CardInstance> Cards => CardList;
            public PendingDecisionContract DecisionContract => null;
            public PendingDecisionOwner DecisionOwner => default;
            public IReadOnlyList<MatchLogEntry> MatchLog => LogRows;
            public MatchUndoMarker UndoMarker => default;
            public PendingInitiativeChoice PendingInitiativeChoice => null;
            public PendingCardActionResolution PendingMovementResolution => null;
            public bool HasPendingMovementResolution => false;
            public PendingMovementChoice PendingMovementChoice => null;
            public PendingAttackResolution PendingAttackResolution => null;
            public bool HasPendingAttackResolution => false;
            public PendingAttackTargetChoice PendingAttackTargetChoice => null;

            public IReadOnlyList<AttackTargetDefensePreviewProjection>
                PendingAttackTargetDefensePreviews =>
                Array.Empty<AttackTargetDefensePreviewProjection>();

            public PendingDefenseResponseChoice PendingDefenseChoice => null;
            public PendingDefensePreviewProjection PendingDefensePreview => null;
            public PendingResolution PendingSkillResolution => null;
            public bool HasPendingSkillResolution => false;
            public PendingEffectChoice PendingEffectChoice => null;

            public IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview =>
                Array.Empty<EffectChoiceCandidatePreview>();

            public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                PendingDisplacedMinionCorrection => null;

            public PendingMinionBattleChoice PendingMinionBattleChoice => null;
            public PendingHeroRespawnChoice PendingHeroRespawnChoice => null;

            public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                PendingSystemTransitionStep => null;

            public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
                PendingSystemTransitionDescription => null;

            public RemovalPreventionPrompt PendingRemovalPrevention => null;
            public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease => null;
            public IReadOnlyList<ActionOffer> AvailableTurnActions => Array.Empty<ActionOffer>();

            public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
                Array.Empty<FastTravelOptionProjection>();

            public IReadOnlyList<ClearOptionProjection> AvailableClearOptions =>
                Array.Empty<ClearOptionProjection>();

            public ActionOffer? AvailableHoldOffer => null;
        }
    }
}

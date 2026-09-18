using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Bain the Bounty Hunter's piece, his tray and his five starting-hand faces, driven through real
    /// pointer events and camera rays. Milestone 8.40k, and the FIFTEENTH hero to take the permanent
    /// player-path gate - the last block of his series.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals and he is not among them, so every arm
    /// that deals him restages both heroes into the engagement milestone 8.15 built - exactly as the
    /// fourteen fixtures before this one do.</para>
    ///
    /// <para><b>THE STAGED BOARD WAS MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, which is the lesson
    /// milestone 8.38f records and the reason the offers below are asserted as exact sets. Milestone
    /// 8.40j wrote the measurement down in <c>tmp/8-40j-staged-board.txt</c>: the engagement stands him
    /// at (-3,1) and the one enemy hero at (-2,2) on a deliberately BENT line, with a friendly Blue
    /// melee minion at (-3,2), an enemy Red ranged minion at (-4,2), enemy Red melee minions at (-1,-1)
    /// and (-1,0), and terrain at (-3,3), (-4,3) and (-3,-2).</para>
    ///
    /// <para><b>AND TWO OF HIS FACES NEED A PIECE THE ENGAGEMENT DOES NOT STAND.</b> His Silver basic
    /// stops at the FIRST obstacle on a file and his three plain crossbows refuse what stands behind a
    /// UNIT, so proving either NEGATIVE needs a second piece behind the first. The Director's own
    /// sandbox spawns one Red melee at (-5,3) - directly behind the Red ranged minion at (-4,2), on the
    /// same straight line out of his space - which is the seam Silverarrow's, Tali's and Cutter's
    /// journeys already use for exactly this kind of arrangement.</para>
    ///
    /// <para><b>What NO journey of his can reach, recorded rather than left as a silence.</b> Both of
    /// his line clauses name TOKENS - "a unit or a token" on the pull (BA-03's wide list) and "no
    /// units, or terrain" on the crossbows (the narrow one, which a token does NOT block) - and no
    /// journey in this build can put a token on a board that deals him. A token reaches the board only
    /// when an effect places one, the token-placing capabilities belong to Tali, and the Wasp-versus-Bain
    /// staging deals neither her nor any other placer;
    /// <see cref="SandboxStagingDescription"/> refuses to stage one by name, on
    /// <c>ComponentLifetimeOwnershipConformanceTests</c>'s governed rule that component lifetime
    /// belongs to the capability handlers. Both token halves are therefore owned at the authoritative
    /// tier - <c>PullLineTargetTests</c> and <c>ObstacleLineTargetTests</c> - and the two journeys
    /// below drive the unit halves, which is the same SHAPE with a different candidate set.</para>
    /// </summary>
    public sealed class BainUiJourneyPlayModeTests
    {
        /// <summary>The Red melee piece the sandbox stands behind the engagement's own ranged minion.</summary>
        internal static readonly MinionId BehindTheFirstObstacle = new MinionId("bain-gate-red-behind");

        /// <summary>Where it stands: one more step along the file that already holds (-4,2).</summary>
        internal static readonly HexCoordinate BehindAt = new HexCoordinate(-5, 3);

        /// <summary>
        /// The staged seat dealt Bain's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState BainSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == BainCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Bain to exactly one seat; it dealt him to " +
                seats.Length + ". A staged arm missing from SelectSetupConfiguration deals his cards " +
                "to a seat whose content identity is somebody else's.");
            return seats[0];
        }

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the pieces where this journey needs them.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the
        /// ids only exist once a configuration has been composed - which is the shape milestone
        /// 8.37a's Guardian Spirit journey minted and every hero since has reused. Nothing else about
        /// the scene changes: a null coordinate leaves a hero where setup put them, and a null hand
        /// leaves the hand the staging dealt.</para>
        /// </summary>
        internal static IEnumerator RestagedScene(
            SceneCardContent content,
            HexCoordinate? bainAt = null,
            HexCoordinate? enemyAt = null,
            IEnumerable<CardDefinitionId> bainHand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            bool spawnBehindTheFirstObstacle = false)
        {
            yield return LoadStagedScene(content);
            if (bainAt == null && enemyAt == null && bainHand == null && enemyHand == null &&
                !spawnBehindTheFirstObstacle)
            {
                yield break;
            }

            var surveyed = Root().Runtime;
            var bainId = BainSeat(surveyed).HeroId;
            var enemyId = OtherHeroId(surveyed, bainId);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(bainId, bainAt, hand: bainHand),
                    new SandboxHeroStaging(enemyId, enemyAt, hand: enemyHand)
                },
                spawnedMinions: spawnBehindTheFirstObstacle
                    ? new[]
                    {
                        new SandboxMinionStaging(
                            BehindTheFirstObstacle, TeamSide.Red, StandardMinionType.Melee, BehindAt)
                    }
                    : null);
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// Drives his named card to the point where its primary action is his to submit, and returns
        /// with the turn standing on him. Every content journey of his opens this way.
        /// </summary>
        internal static IEnumerator HisTurnOn(
            MatchSceneCompositionRoot root, HeroId bainId, CardDefinitionId definitionId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, bainId, definitionId);
            yield return YieldUntilTheNamedSeatActs(runtime, bainId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(bainId),
                "His card has to open the turn for this journey to drive it. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the effect control whose own label the caller names, then presses the confirm that
        /// stands behind it when one does.
        ///
        /// <para><b>Selecting is not taking</b>, which milestone 8.35z53's first Play Mode run
        /// recorded: a <c>Choose</c> stands on Deselect/Confirm until the electing seat commits. The
        /// confirm is looked up again after the select rather than remembered, because the model is
        /// rebuilt.</para>
        /// </summary>
        /// <remarks>
        /// <para><b>Milestone 8.46c: both halves now read the DECISION WINDOW's own model.</b> The
        /// effect-choice controls moved off the round-action strip onto the window, and milestone
        /// 8.46a is replacing every one of their labels with the consequence the content declares -
        /// so a journey that pressed a literal string, or read a label off a model that is no longer
        /// the one drawing the control, would press the wrong thing the moment those labels land.
        /// The window's own control is what a player presses, so it is what these read.</para>
        /// </remarks>
        internal static IEnumerator PressEffectControl(MatchSceneCompositionRoot root, string label)
        {
            var runtime = root.Runtime;
            var standing = runtime.DecisionWindowView.Model?.DecisionKey;
            UiPlayer.Click(UiPlayer.Button(root.Hud, label));
            yield return null;
            yield return null;

            // A choose-several still keeps its DONE; an exactly-one no longer has one at all,
            // because the window follows the select with the confirm the authority itself offered.
            // The confirm is looked up again after the select rather than remembered, and only while
            // the SAME decision is still standing - otherwise a press would land on whatever question
            // the answered one opened next.
            var model = runtime.DecisionWindowView.Model;
            var confirm = model != null && model.DecisionKey == standing
                ? model.Controls.FirstOrDefault(control =>
                    control.Role == DecisionWindowControlRole.Confirm)
                : null;
            if (confirm != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
                yield return null;
                yield return null;
            }
        }

        /// <summary>The label of the option control standing for the named branch node.</summary>
        internal static string BulletLabelFor(
            ConfiguredMatchSceneRuntime runtime, string optionNodeId)
        {
            var model = runtime.DecisionWindowView.Model;
            Assert.That(model, Is.Not.Null,
                $"No decision window stands, so nothing answers '{optionNodeId}'. " +
                Remaining(runtime));
            var bullets = model.Controls
                .Where(control => control.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    control.Command.EffectChoice.Candidate.OptionNodeId.IsDefined)
                .ToArray();
            var match = bullets.FirstOrDefault(control =>
                control.Command.EffectChoice.Candidate.OptionNodeId.Value == optionNodeId);
            Assert.That(match, Is.Not.Null,
                $"No standing bullet answers '{optionNodeId}'. Standing: " +
                string.Join(", ", bullets.Select(control =>
                    control.Command.EffectChoice.Candidate.OptionNodeId.Value + "='" + control.Label +
                    "'")) + ". " + Remaining(runtime));
            return match.Label;
        }

        /// <summary>The coordinates a standing pause is offering, unordered, read off its candidates.</summary>
        internal static HexCoordinate[] OfferedSpaces(ConfiguredMatchSceneRuntime runtime)
        {
            var pause = runtime.PendingEffectChoice;
            Assert.That(pause, Is.Not.Null,
                "No pause is standing to read an offer from. " + Remaining(runtime));
            return pause.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
        }

        /// <summary>
        /// Answers a card-valued effect choice by clicking the WHOLE PRINTED FACE, which is the
        /// contract milestone 8.15a made every card-valued choice in this build carry. Returns the
        /// instance id of the face that was clicked through <paramref name="clicked"/>.
        /// </summary>
        internal static IEnumerator ClickACardFace(
            MatchSceneCompositionRoot root, System.Action<CardInstanceId> clicked = null)
        {
            var runtime = root.Runtime;
            var choices = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(choices, Is.Not.Empty,
                "A card-valued choice draws one control per face. Shown controls: " +
                UiPlayer.Describe(root.Hud) + ". " + Remaining(runtime));
            Assert.That(choices.Select(value => value.Card), Has.All.Not.Null,
                "Each control is the whole printed face, never an anonymous ordinal.");

            var faceName = choices[0].Card.DisplayName.ToUpperInvariant();
            clicked?.Invoke(choices[0].Command.EffectChoice.Candidate.Value.CardInstanceId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            yield return null;
        }

        internal static int HandCountOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards.Count(card =>
                card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand);

        /// <summary>
        /// Answers p.14's Defense response by clicking the named face where the prompt draws it - a
        /// card ELEMENT rather than a labelled button, which milestone 8.35z38's dealing found the
        /// hard way and every gate since has clicked by name.
        /// </summary>
        internal static IEnumerator ClickDefenseFace(
            MatchSceneCompositionRoot root, CardDefinitionId definitionId)
        {
            var runtime = root.Runtime;
            var faceName = NameOf(runtime, definitionId);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(root.Hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Opens the Director drawer only when it is shut. <b>It TOGGLES, and its own trigger's label
        /// changes when it is open</b>, so a blind second click closes a drawer an earlier step
        /// already opened. RESET is the tell, because it is drawn only while the drawer is open.
        /// </summary>
        internal static void OpenDirectorDrawer(ConfiguredMatchHudRenderer hud)
        {
            if (!UiPlayer.HasButton(hud, "RESET"))
            {
                UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            }
        }

        /// <summary>Every bounty attachment the projection currently holds, as holder ids.</summary>
        internal static string[] BountyHolders(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.HeldMarkers
                .Where(marker => marker.Definition.Id.Equals(BainComponentContent.Bounty))
                .Select(marker => marker.HolderHeroId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

        // -------------------------------------------------------------------------------------------
        // The component half his piece owns, on his own dealt board
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HisTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;

            // The highest-Initiative card of his printing a boot. Get over here! prints NO boot at all
            // - its one ribbon icon is a Defense shield drawn in the boot's slot, which is BA-05's trap
            // at the roster's largest scale (SIX faces of his print one icon, three shields and three
            // boots, all in the same slot) - so the helper choosing by printed value is what keeps this
            // journey off it.
            yield return CommitEverySeat(root, bainId, MovingCardOf(runtime, bainId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(bainId));
            Assert.That(runtime.GetCommitmentStatus(bainId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableSpace(
                pointerInput,
                runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 1)
                    .Select(value => value.Destination)
                    .ToArray());
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate,
                Is.EqualTo(destination),
                "His piece follows his authoritative space after a walk a player drove.");

            if (runtime.PendingMovementChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(bainId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        /// <summary>
        /// p.14's Defense response and p.13's Defeat and respawn, on his own piece.
        ///
        /// <para><b>The enemy is STOOD beside him rather than walked there</b>, which is milestone
        /// 8.37a's own correction of an arc that cost turns for nothing: the engagement is two spaces
        /// wide on a BENT line, an Attack needs contact, and a gap-closing turn proves nothing this
        /// journey is about. The sandbox stands him on (-2,1), a free neighbour of Bain's staged space.
        /// </para>
        ///
        /// <para><b>Close Call is spent as the commitment rather than kept</b>, deliberately: this
        /// journey is about the ordinary SECONDARY Defense icon twelve of his faces print, and his
        /// Green Blocks have three journeys of their own. Four of his five starting cards print a
        /// Defense secondary, so the offer here is never empty.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisDefenseAnswersAnAttack_AndHisPieceRespawnsWhenItDoesNot()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedBain,
                enemyAt: new HexCoordinate(-2, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(
                HexDistanceQuery.Calculate(
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate,
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate),
                Is.EqualTo(1),
                "The whole arc depends on his Attack needing no turn to reach him.");

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, bainId, BainCardContent.CloseCall);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before his own card. " + Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(bainId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers, Is.Not.Empty,
                "His hand holds Defense secondaries, so the offer is not empty.");
            Assert.That(
                runtime.PendingDefenseChoice.LegalDefenseOffers.All(offer =>
                    OriginOf(runtime, offer) == ActionDefinitionOrigin.Reaction),
                Is.True,
                "and every one of them is the secondary icon, because the only Defense PRIMARY he " +
                "prints is a Green Block and this journey spent the one he was dealt. " +
                Remaining(runtime));

            // The offer is answered on the control the prompt draws beside those faces rather than
            // through the runtime's own method, because what this row claims is that a player meets
            // the choice - and declining is what makes this Attack a Defeat regardless of either
            // printed value.
            // Milestone 8.46e: the Defense is drawn on the decision window, whose corner names the
            // consequence - read off its model rather than the strip's, which no longer draws it.
            UiPlayer.ClickDefenseDecline(runtime, hud);
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(bainId)), Is.False,
                "The defeated hero is off the board until he respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(bainId)), Is.False);

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, bainId, BainCardContent.LightCrossbow);
            yield return YieldUntilTheNamedSeatActs(runtime, bainId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(bainId),
                "The respawn is offered at his own turn, so his card has to open it. " +
                Remaining(runtime));

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Bain must be offered his respawn at his own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(bainId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Bain token stands after the respawn, not a second beside a stale one.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // His five starting-hand faces
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dead or Alive: "Target a unit adjacent to you. After the attack: You may give an enemy hero
        /// in radius a bounty marker; a hero with a bounty marker spends 1 additional life counter when
        /// defeated."
        ///
        /// <para><b>Four shapes in one page and the order is the printed one.</b> p.14 asks whom first
        /// - and on the untouched engagement exactly ONE unit stands beside him, the Red ranged minion
        /// at (-4,2), which is what makes the target offer an exact set rather than a sample. The
        /// attack then removes it, which never opens p.14's step 3 window, and the card's own "After
        /// the attack:" clause stops the page on p.10's `may`. Confirming it opens the radius-4
        /// selection, whose one candidate is the enemy hero two spaces away, and answering THAT is the
        /// give.</para>
        ///
        /// <para><b>The reading is the point of the last half.</b> His bounty declares one property
        /// whose value channel is None, so the holder's rail must draw the caption alone - no chip, no
        /// empty number - and the giver's rail must stay empty. Milestone 8.40j proved the rail can
        /// draw it; this journey proves a PLAYER can cause it and read it, which is what three of his
        /// five Green faces then decide from.</para>
        ///
        /// <para><b>And the retained-prefix Undo runs on the LIVE session here</b>, before any snapshot
        /// is taken: <c>RestoreSnapshot</c> clears the accepted history, so an Undo attempted after a
        /// restore would be asking for a control the product is right to withhold. One press of the
        /// drawer's own UNDO takes the give back, the pause it was answering stands again, and both
        /// rails are empty - and the same click then gives it a second time.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeadOrAliveGivesTheBountyAfterItsAttack_AndTheHoldersRailNamesIt()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the staged position");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the staged position");

            yield return HisTurnOn(root, bainId, BainCardContent.DeadOrAlive);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var targets = runtime.PendingAttackTargetChoice;
            Assert.That(targets, Is.Not.Null,
                "His Gold basic opens p.14's target question. " + Remaining(runtime));
            var offered = targets.LegalTargets
                .Select(target => target.Kind == AttackTargetKind.Hero
                    ? "hero " + target.HeroId.Value
                    : runtime.ReadModel.Minions
                        .Single(minion => minion.MinionId.Equals(target.MinionId))
                        .Coordinate.ToString())
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            Assert.That(offered, Is.EqualTo(new[] { new HexCoordinate(-4, 2).ToString() }).AsCollection,
                "\"Target a unit adjacent to you\" admits the one ENEMY unit standing beside him on " +
                "this board, and neither the friendly minion at (-3,2) nor the enemy hero two away. " +
                "Offer was: " + string.Join(", ", offered));

            var victim = runtime.ReadModel.Minions
                .Single(minion => minion.Coordinate.Equals(new HexCoordinate(-4, 2)));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(victim.MinionId).transform.position),
                Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            // p.14's own SECOND moment - the card text after the attack has been applied.
            var may = runtime.PendingEffectChoice;
            Assert.That(may, Is.Not.Null,
                "\"After the attack:\" stops the page once p.14 has applied its consequences. " +
                Remaining(runtime));
            Assert.That(may.NodeId.Value, Is.EqualTo("bounty-may"), Remaining(runtime));
            Assert.That(may.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "p.10's `may` is a confirm-or-decline and not a selection. " + Remaining(runtime));
            Assert.That(may.ChooserId, Is.EqualTo(bainId),
                "An unassigned decision belongs to the card's controller. " + Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "and it really is declinable, which is what \"you may\" prints. " + Remaining(runtime));

            var confirm = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;
            yield return null;

            var selection = runtime.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null,
                "Taking the `may` opens the radius selection. " + Remaining(runtime));
            Assert.That(selection.NodeId.Value, Is.EqualTo("bounty-select"), Remaining(runtime));
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "\"an enemy hero in radius\" names the one enemy hero inside the printed Radius 4. " +
                Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The recipient is named by clicking the piece where they stand.");
            yield return null;
            yield return null;

            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                "The give put his one bounty on the hero he chose. " + Remaining(runtime));
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the answered give", "BOUNTY");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the answered give");

            // RETAINED-PREFIX UNDO, on the live session and through the drawer's own control.
            //
            // Milestone 8.46b: ONE BOARD CLICK ON A CHOICE OF EXACTLY ONE IS ONE ACCEPTED COMMAND -
            // the select carries the confirm the authority offered behind it - so one press gives
            // the question back with nothing selected on it, and a SECOND press reaches past it to
            // the printed `may` the selection was opened by. Both states are asserted rather than
            // skipped, because each says what an Undo retracts.
            OpenDirectorDrawer(hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            yield return null;

            Assert.That(BountyHolders(runtime), Is.Empty,
                "The first UNDO takes the give back, because a retained-prefix Undo drops accepted " +
                "commands from the end. " + Remaining(runtime));
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the undone give");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the undone give");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the resolution is standing on the question the undone answer was answering. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("bounty-select"),
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Is.Empty,
                "and nothing is selected on it, because the one press that answered it was the one " +
                "command the Undo dropped. " + Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("bounty-may"),
                "The second press retracts the taken `may`, the command before the give. " +
                Remaining(runtime));

            // Take the `may` again on the window's own control, which reopens the same selection.
            UiPlayer.ClickEffectControl(runtime, hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("bounty-select"),
                "Taking the `may` again reopens the radius selection, unanswered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Is.Empty,
                "and nothing is selected on it, which is what makes the click below the " +
                "same click rather than a second one. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "and the restored question takes the same click it took before. " +
                UiPlayer.DescribeBoardRay(pointerInput, HeroView(enemyId).transform.position));
            yield return null;
            yield return null;
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                "The re-answered give is the same give. viewed=" + runtime.ViewedHeroId.Value +
                "; " + UiPlayer.DescribeBoardRay(pointerInput, HeroView(enemyId).transform.position) +
                "; " + Remaining(runtime));
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the re-answered give", "BOUNTY");
        }

        /// <summary>
        /// Get over here!: "Target a unit or a token in a straight line and in range with no obstacles
        /// between you and the target; move the target towards you in a straight line, until you are
        /// adjacent."
        ///
        /// <para><b>The OFFER is most of this face and it is asserted as an exact set.</b> p.19's six
        /// files each contribute at most their FIRST obstacle, and BA-03's wide list means a token
        /// blocks as a unit does - so what is offered out of his staged space is the Red ranged minion
        /// at (-4,2), the friendly Blue melee at (-3,2) (the printed noun carries no side-word) and the
        /// Red melee at (-1,-1). Three things are deliberately NOT offered and each says something
        /// different: the Red melee the sandbox stands at (-5,3), which is on the same file BEHIND the
        /// first obstacle; the terrain at (-3,-2), which is the first obstacle on its own file and is
        /// excluded from being NAMED because the printed noun is "a unit or a token"; and the terrain
        /// at (-3,3), which is both.</para>
        ///
        /// <para><b>The far target is the one this journey takes</b>, because "until you are adjacent"
        /// then has two spaces of work to do: the Red melee at (-1,-1) is pulled to (-2,0), the first
        /// space on the file that touches him.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator GetOverHerePullsTheFirstUnitOnItsFile_UntilItStandsBesideHim()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedBain, spawnBehindTheFirstObstacle: true);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate;
            var pulled = new HexCoordinate(-1, -1);
            var landsOn = new HexCoordinate(-2, 0);

            Assert.That(
                runtime.ReadModel.Minions.Any(minion =>
                    minion.MinionId.Equals(BehindTheFirstObstacle) &&
                    minion.Coordinate.Equals(BehindAt)),
                Is.True,
                "The sandbox piece behind the first obstacle is what makes the negative half of this " +
                "offer provable at all.");

            yield return HisTurnOn(root, bainId, BainCardContent.GetOverHere);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "His Silver basic asks which piece. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("pull-target"),
                Remaining(runtime));
            var offer = OfferedSpaces(runtime);
            var shape = string.Join(", ", offer.Select(value =>
                value + "/d" + HexDistanceQuery.Calculate(from, value)));

            Assert.That(offer, Is.EqualTo(new[]
                {
                    new HexCoordinate(-4, 2), new HexCoordinate(-3, 2), pulled
                }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "Each of p.19's six files offers at most the FIRST obstacle along it, and nothing " +
                "behind one. Offer was: " + shape);
            Assert.That(offer, Has.No.Member(BehindAt),
                "The unit the sandbox stands one step behind (-4,2) is inside the printed Range 4 " +
                "and on the same straight line, and it is not offered - which is what \"with no " +
                "obstacles between you and the target\" means. Offer was: " + shape);
            Assert.That(offer, Has.No.Member(new HexCoordinate(-3, -2)),
                "The terrain three along an otherwise empty file is the first obstacle there, so it " +
                "BLOCKS - and the printed noun is \"a unit or a token\", so it cannot be NAMED and " +
                "that file offers nothing at all. Offer was: " + shape);
            foreach (var candidate in offer)
            {
                Assert.That(StraightLineQuery.AreInStraightLine(from, candidate), Is.True,
                    "Every offered piece stands on one of p.19's straight lines out of his own " +
                    "space. Offer was: " + shape);
                Assert.That(HexDistanceQuery.Calculate(from, candidate), Is.InRange(1, 4),
                    "and inside the printed Range 4. Offer was: " + shape);
            }

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(pulled).transform.position), Is.True,
                "The pulled piece is named by clicking the space it stands on. " +
                UiPlayer.DescribeBoardRay(pointerInput, Tile(pulled).transform.position));
            yield return null;
            yield return null;

            Assert.That(
                runtime.ReadModel.Minions.Single(minion =>
                    minion.Coordinate.Equals(landsOn)).Coordinate,
                Is.EqualTo(landsOn),
                "\"move the target towards you in a straight line, until you are adjacent\" walked " +
                "it the one space between and stopped it beside him. " + Remaining(runtime));
            Assert.That(
                HexDistanceQuery.Calculate(
                    from, runtime.ReadModel.Minions
                        .Single(minion => minion.MinionId.Equals(
                            runtime.ReadModel.Minions.Single(value =>
                                value.Coordinate.Equals(landsOn)).MinionId))
                        .Coordinate),
                Is.EqualTo(1),
                "and it really is adjacent to him now. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The face is finished: the Move names its own destination and asks nothing. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Light Crossbow: "Target a unit in a straight line and in range with no units, or terrain,
        /// between you."
        ///
        /// <para><b>The NARROW line clause is the whole of what separates his three plain crossbows
        /// from his Silver basic, and BA-03 records it as the most consequential source fact in his
        /// inventory.</b> p.18's Obstacle keyword is strictly wider than "units, or terrain", so what
        /// is asserted here is the half a token would separate: a UNIT between him and a target refuses
        /// it. The sandbox stands a Red melee at (-5,3), two along the file that already holds the Red
        /// ranged minion at (-4,2) and inside the printed Range 2 - and it is not offered, while the
        /// piece in front of it is.</para>
        ///
        /// <para><b>The token half of the same sentence is UNREACHABLE in any journey of this build</b>
        /// and is recorded on this fixture rather than papered over: no content dealt into a
        /// Wasp-versus-Bain match places a token, and the Director's sandbox refuses to stage one by
        /// name. <c>ObstacleLineTargetTests</c> and his own printed-set fixture own it at the
        /// authoritative tier.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LightCrossbowShootsDownAClearFile_AndRefusesWhatStandsBehindAUnit()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedBain, spawnBehindTheFirstObstacle: true);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate;
            var shotAt = new HexCoordinate(-4, 2);

            yield return HisTurnOn(root, bainId, BainCardContent.LightCrossbow);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null,
                "His Tier I Red opens p.14's target question with no card text in front of it. " +
                Remaining(runtime));
            var offered = choice.LegalTargets
                .Select(target => runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(target.MinionId)).Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
            Assert.That(offered, Is.EqualTo(new[]
                {
                    shotAt, new HexCoordinate(-1, -1)
                }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "\"in a straight line and in range\" admits exactly the enemy units on a file within " +
                "the printed Range 2. Offer was: " +
                string.Join(", ", offered.Select(value => value.ToString())));
            Assert.That(offered, Has.No.Member(BehindAt),
                "and \"with no units, or terrain, between you\" refuses the piece standing two along " +
                "the file the Red ranged minion already occupies - which is inside Range 2 and on a " +
                "straight line, so the line clause is the only thing that can be stopping it.");
            foreach (var candidate in offered)
            {
                Assert.That(StraightLineQuery.AreInStraightLine(from, candidate), Is.True);
                Assert.That(HexDistanceQuery.Calculate(from, candidate), Is.InRange(1, 2));
            }

            var victim = runtime.ReadModel.Minions.Single(minion => minion.Coordinate.Equals(shotAt));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(victim.MinionId).transform.position),
                Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.Coordinate.Equals(shotAt)), Is.False,
                "Attack 5 against a minion removes it, which is p.14 read literally. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The face carries no card text at all, so nothing stands after the page. " +
                Remaining(runtime));
        }

        /// <summary>
        /// A Game of Chance: "An enemy hero in radius with two or more cards in hand chooses one of
        /// those cards; Guess the card's color, then reveal the card; If you guessed correctly, discard
        /// that card; otherwise you gain 1 coin."
        ///
        /// <para><b>THREE THINGS HERE HAVE NEVER BEEN DRIVEN BY ANY JOURNEY IN THIS BUILD.</b> A card
        /// is chosen from an ENEMY's seat and KEPT; the controller answers a different question against
        /// it; and a reveal then changes what every seat can see, mid-resolution. The first is asserted
        /// by its chooser - <c>EffectChoiceRequest.ChooserHeroId</c> is the field
        /// <c>SeatProjection.OwnerOfStandingDecision</c> reads, so a chooser that fell back to the
        /// actor would silently hand the enemy's choice to the card's own controller and would look
        /// like a working prompt from Bain's side of the table. The third is asserted on the match
        /// LOG, which is the seat-public surface the reveal actually writes to: before the reveal no
        /// row in the log names the chosen card, and after it exactly one does - and that row is what
        /// <c>SeatProjection.IsPublic</c>'s hand arm reads to make the card nameable in every seat.
        /// </para>
        ///
        /// <para><b>And the snapshot is taken BETWEEN the hidden choice and the reveal</b>, which is
        /// the one place in this build where a restore has to reconstruct a pause whose answer only one
        /// seat may see. The restore lands back on the colour question with the hidden answer intact,
        /// and the same guess then resolves.</para>
        ///
        /// <para><b>The guess is deliberately CORRECT here</b> - the journey reads the chosen card's
        /// own printed colour and names it - so the discard branch runs and exactly that card leaves
        /// the hand. Dead Man's Hand's journey takes the other branch.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AGameOfChanceHidesTheEnemysCard_GuessesIt_AndRevealsItToEverySeat()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return HisTurnOn(root, bainId, BainCardContent.AGameOfChance);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            // HIS OWN QUESTION FIRST: "an enemy hero in radius with two or more cards in hand".
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null,
                "His Tier I Blue opens with the selection that names whose hand this is about. " +
                Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("guess-select"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, bainId), Is.True,
                "He picks WHICH enemy hero; the pick beneath it is the only part he does not own. " +
                Remaining(runtime));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "The one enemy hero inside the printed Radius 2 holding two or more cards. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            // THE HIDDEN PICK, AND IT IS NOT HIS.
            var hidden = runtime.PendingEffectChoice;
            Assert.That(hidden, Is.Not.Null,
                "\"chooses one of those cards\" opens once he has named whose hand it is. " +
                Remaining(runtime));
            Assert.That(hidden.NodeId.Value, Is.EqualTo("guess-hide"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The pending choice's chooser is what SeatProjection.OwnerOfStandingDecision reads, " +
                "so it is the field that decides which seat is shown these faces. " +
                Remaining(runtime));
            Assert.That(hidden.ChooserId, Is.Not.EqualTo(bainId),
                "and a chooser that fell back to the actor would hand the enemy's own decision to " +
                "the guesser, which is exactly the defect this assertion exists to catch. " +
                Remaining(runtime));
            Assert.That(
                hidden.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemyId)),
                Is.True,
                "Every offered face is one of THEIR OWN unplayed hand cards. " + Remaining(runtime));
            Assert.That(hidden.Candidates.Count, Is.GreaterThanOrEqualTo(2),
                "and the printed clause narrowed the selection to a hero holding two or more. " +
                Remaining(runtime));

            var revealRowsBefore = RevealRows(runtime).Length;
            Assert.That(revealRowsBefore, Is.Zero,
                "Nothing has been revealed by card text yet. " + Remaining(runtime));

            var chosen = default(CardInstanceId);
            yield return ClickACardFace(root, card => chosen = card);
            Assert.That(chosen.IsDefined, Is.True);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.Hand),
                "The chosen card is KEPT: the pick publishes an identity and moves nothing. " +
                Remaining(runtime));
            Assert.That(RevealRows(runtime), Is.Empty,
                "and nothing about it has reached the seat-public log, because the guess has not " +
                "been named yet. " + Remaining(runtime));

            // SNAPSHOT RESTORE, taken between the hidden answer and the reveal.
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            var colour = runtime.PendingEffectChoice;
            Assert.That(colour, Is.Not.Null,
                "\"Guess the card's color\" is the next question. " + Remaining(runtime));
            Assert.That(colour.NodeId.Value, Is.EqualTo("guess-name-colour"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, bainId), Is.True,
                "and the guess is HIS - \"you\" on a card is its controller. " + Remaining(runtime));
            var domain = colour.Candidates
                .Select(candidate => (CardColor)candidate.Value.IntegerValue)
                .OrderBy(value => value)
                .ToArray();
            Assert.That(domain, Has.No.Member(CardColor.Purple),
                "BA-D2: p.9's Purple is the Ultimate, which never enters a hand, so it is never a " +
                "legal guess. Offer was: " + string.Join(", ", domain));
            Assert.That(domain, Has.No.Member(CardColor.None));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The payload taken between the pick and the guess restores.");
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guess-name-colour"),
                "and it is the SAME question the capture was taken on, answerable on the same " +
                "surface. " + Remaining(runtime));
            Assert.That(
                runtime.PendingEffectChoice.Candidates
                    .Select(candidate => (CardColor)candidate.Value.IntegerValue)
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(domain).AsCollection,
                "and the restored offer is the same offer, which means the hidden answer came back " +
                "with it rather than being re-derived from a hand the restore could not see. " +
                Remaining(runtime));

            var printed = runtime.Definitions
                .Single(definition => definition.Reference.Equals(
                    runtime.Cards.Single(card => card.Id.Equals(chosen)).DefinitionReference));
            var faceName = printed.DisplayName.ToUpperInvariant();

            // THE PUBLIC SURFACE IS OPENED BEFORE THE GUESS RATHER THAN AFTER IT, which is both
            // truer to a player and the only way to read it: the seat-public match log is the memory
            // `SeatProjection.IsPublic` consults, and its toggle is drawn while a turn is being
            // resolved rather than while the table is committing.
            // Its toggle is a drawn GLYPH on the status strip rather than a labelled button, so it is
            // pressed as the element a player's pointer actually lands on.
            Assert.That(UiPlayer.IsShown(runtime.MatchLogView.ToggleRoot), Is.True,
                "The match log's toggle is drawn where a player can reach it. Shown: " +
                UiPlayer.Describe(hud));
            UiPlayer.Click(runtime.MatchLogView.ToggleRoot);
            yield return null;
            yield return null;
            Assert.That(runtime.MatchLogView.Model.IsOpen, Is.True,
                "The log a player opens is open. " + Remaining(runtime));
            Assert.That(LinesNaming(runtime, faceName, "REVEALED"), Is.Empty,
                "and nothing in it names the hidden card yet, which is the whole of what makes the " +
                "answer hidden. Lines: " + string.Join(" | ", LogLines(runtime)));

            var correct = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.Value.IntegerValue ==
                    (int)printed.Progression.Color);
            yield return PressEffectControl(root, correct.Label);

            // THE REVEAL, READ ON THE SURFACE EVERY SEAT HAS.
            var revealed = RevealRows(runtime);
            Assert.That(revealed, Has.Length.EqualTo(1),
                "\"then reveal the card\" writes exactly one seat-public row. " + Remaining(runtime));
            Assert.That(revealed[0].SubjectHeroId, Is.EqualTo(enemyId),
                "and the row names the card's OWNER rather than the hero who guessed. " +
                Remaining(runtime));
            Assert.That(revealed[0].SubjectCardInstanceId, Is.EqualTo(chosen),
                "and it names the card that was actually hidden. " + Remaining(runtime));

            // MEASURED, AND RECORDED RATHER THAN ASSERTED AWAY. Milestone 8.35z7's click-away rule
            // closes the drop-down on any pointer-down that is not its own, and the colour control is
            // one - so the one surface in this build that NAMES a reveal shuts at the exact moment
            // the reveal is written into it, and a player who opened the log to watch their own guess
            // has to open it again to read the answer. Raised in the completion evidence rather than
            // changed here: the click-away is a user-directed affordance shared by every drop-down in
            // the HUD, so narrowing it is its own milestone. If this ever becomes true the rule has
            // been narrowed and this assertion is what will say so.
            var stayedOpen = runtime.MatchLogView.Model.IsOpen;
            Assert.That(stayedOpen, Is.False,
                "Recording the measured state: answering the guess closes the log it is written to.");
            UiPlayer.Click(runtime.MatchLogView.ToggleRoot);
            yield return null;
            yield return null;

            Assert.That(runtime.MatchLogView.Model.IsOpen, Is.True, Remaining(runtime));
            Assert.That(LinesNaming(runtime, faceName, "REVEALED"), Is.Not.Empty,
                "and the log now says so BY NAME, which is what makes the reveal a thing the table " +
                "can read rather than a fact in the host's memory. Lines: " +
                string.Join(" | ", LogLines(runtime)));

            Assert.That(runtime.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "\"If you guessed correctly, discard that card\" spends exactly the card that was " +
                "hidden, and no prompt is opened to choose a different one. " + Remaining(runtime));
        }

        /// <summary>
        /// Close Call: "If a hero in play has a bounty marker, block the attack and that hero gives the
        /// marker to you. (The marker's effect is applied to you)."
        ///
        /// <para><b>A Block that succeeds on a board fact, answered from the DEFENDER's seat.</b> The
        /// first half of this journey ARMS the board - Dead or Alive's whole page, driven headlessly,
        /// because the give has its own journey above and repeating it here would cost a Unity minute
        /// to prove the same thing twice. The second half is the subject: the enemy attacks, p.14 draws
        /// the Defense response, and Close Call is offered as a whole FACE in it because his Green
        /// prints a Defense PRIMARY rather than a secondary icon.</para>
        ///
        /// <para><b>The marker is read BEFORE the Block is answered</b>, which is the sentence the
        /// block's own condition is made of: a player who cannot see who is wearing the bounty cannot
        /// decide whether this face does anything. It is read again afterwards, on the other rail,
        /// because the printed consequence is that the marker comes to HIM.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CloseCallBlocksTheAttack_AndPullsTheBountyOntoHisOwnRail()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedBain,
                enemyAt: new HexCoordinate(-2, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);

            yield return ArmTheBounty(root, bainId, enemyId);
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                "The setup half has to leave the bounty on the enemy or the Block's condition is " +
                "false and this journey would prove the wrong thing. " + Remaining(runtime));

            yield return AdvanceToNextTurn(runtime);

            // He KEEPS Close Call and commits his lowest-Initiative other card, so p.7 gives the
            // enemy's Attack the turn and the Green is still in hand to be offered as a Defense.
            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, bainId,
                LowestInitiativeIdOf(runtime, bainId, BainCardContent.CloseCall));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before his own card. " + Remaining(runtime));

            // THE READING THE BLOCK IS DECIDED FROM, taken before anything is answered.
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the armed board", "BOUNTY");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the armed board");

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(bainId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;
            yield return null;

            var defense = runtime.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var block = defense.LegalDefenseOffers.FirstOrDefault(offer =>
                runtime.Cards.Single(card => card.Id.Equals(offer.SourceCardInstanceId))
                    .DefinitionReference.DefinitionId.Equals(BainCardContent.CloseCall));
            Assert.That(block.SourceCardInstanceId.IsDefined, Is.True,
                "Close Call is offered as a whole face in the defender's prompt because it prints a " +
                "Defense PRIMARY, and it is offered because a hero in play wears his bounty. " +
                "Offered: " + string.Join(", ", defense.LegalDefenseOffers.Select(offer =>
                    runtime.Cards.Single(card => card.Id.Equals(offer.SourceCardInstanceId))
                        .DefinitionReference.DefinitionId.Value)) + ". " + Remaining(runtime));
            Assert.That(OriginOf(runtime, block), Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));

            yield return ClickDefenseFace(root, BainCardContent.CloseCall);

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(bainId)), Is.True,
                "\"block the attack\" succeeds regardless of the Attack Value, so he is standing. " +
                Remaining(runtime));
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { bainId.Value }).AsCollection,
                "\"that hero gives the marker to you\" MOVED the one attachment rather than minting " +
                "a second, which is why his own set never puts two bounties on a board. " +
                Remaining(runtime));

            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the answered Block", "BOUNTY");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the answered Block");
        }

        // -------------------------------------------------------------------------------------------
        // Steps shared with the progressed fixture
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Plays Dead or Alive end to end through the projected-command surface, so a journey whose
        /// subject is a marker CONDITION starts from a board that has one. It is setup rather than
        /// subject: the give itself is driven through real controls by
        /// <c>DeadOrAliveGivesTheBountyAfterItsAttack_AndTheHoldersRailNamesIt</c> above.
        /// </summary>
        internal static IEnumerator ArmTheBounty(
            MatchSceneCompositionRoot root, HeroId bainId, HeroId enemyId,
            HexCoordinate? victimAt = null)
        {
            var runtime = root.Runtime;
            yield return HisTurnOn(root, bainId, BainCardContent.DeadOrAlive);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .Where(candidate => candidate.Kind != AttackTargetKind.Hero)
                .First(candidate => victimAt == null || runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(candidate.MinionId))
                    .Coordinate.Equals(victimAt.Value));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.MinionId)),
                Is.True,
                "The setup attack takes a MINION, because defeating the enemy hero would take the " +
                "give's own recipient off the board. " + Remaining(runtime));
            yield return null;
            if (runtime.PendingDefenseChoice != null)
            {
                Assert.That(runtime.DeclineDefense(), Is.True);
                yield return null;
            }

            var may = runtime.PendingEffectChoice;
            Assert.That(may, Is.Not.Null,
                "The setup needs the \"After the attack:\" clause standing. " + Remaining(runtime));
            Assert.That(runtime.ConfirmEffectChoice(), Is.True, Remaining(runtime));
            yield return null;

            // The recipient's own selection is answered through the pause's own minted invocation
            // rather than through a labelled control: a candidate whose subject STANDS ON THE MAP is
            // drawn as a board target and never as a button, which is the standing board-first
            // contract and is exactly what the give's journey answers with a real board click.
            var selection = runtime.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null,
                "The setup needs the give's own selection standing. " + Remaining(runtime));
            var give = selection.SelectInvocations.Single(value =>
                value.Candidate.Value.Kind == EffectValueKind.Hero &&
                value.Candidate.Value.HeroId.Equals(enemyId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(give)), Is.True,
                Remaining(runtime));
            yield return null;

            // Milestone 8.46b: whether a second command is owed is asked rather than assumed. The
            // give's own selection admits exactly one recipient, so the session closes it inside
            // the Select and the guard below finds nothing left standing on that node; a wider
            // selection reached through here still owes its confirm and the setup still presses it.
            var standing = runtime.PendingEffectChoice;
            if (standing != null && standing.NodeId.Equals(selection.NodeId) &&
                standing.ConfirmInvocation != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(standing.ConfirmInvocation)),
                    Is.True,
                    Remaining(runtime));
                yield return null;
            }

            yield return null;

            // AND ON A TIER III BOARD THE GIVE IS NOT THE END OF THE PAGE. A Complicated Profession
            // is faceup in his own area there, so his give fires it and the RECIPIENT is asked for a
            // card before anybody's turn can end - which the Ultimate's own journey drives through
            // real controls, and which every setup arm has to answer or the round never turns over.
            var trigger = runtime.PendingEffectChoice;
            if (trigger != null && trigger.NodeId.Value == "profession-discard")
            {
                var lose = trigger.SelectInvocations.First();
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(lose)),
                    Is.True,
                    Remaining(runtime));
                yield return null;
                yield return ConfirmIfStillStanding(root, trigger.NodeId);
                yield return null;
            }
        }

        /// <summary>The drawn match-log lines, upper-cased, as a player reads them.</summary>
        internal static string[] LogLines(ConfiguredMatchSceneRuntime runtime) =>
            runtime.MatchLogView.Model.Lines
                .Select(line => line.Text.ToUpperInvariant())
                .ToArray();

        /// <summary>The drawn log lines naming both words, which is how a reveal reads on screen.</summary>
        internal static string[] LinesNaming(
            ConfiguredMatchSceneRuntime runtime, string face, string verb) =>
            LogLines(runtime)
                .Where(line => line.Contains(verb) && line.Contains(face))
                .ToArray();

        /// <summary>Every seat-public row the card-text reveal has written, newest last.</summary>
        internal static MatchLogEntry[] RevealRows(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Projection.MatchLog
                .Where(entry => entry.Kind == MatchLogEntryKind.CardRevealedByCardText)
                .ToArray();
    }
}

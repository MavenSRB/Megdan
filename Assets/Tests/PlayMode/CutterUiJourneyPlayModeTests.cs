using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Cutter the Sky Pirate's piece, her tray and her five starting-hand faces, driven through real
    /// pointer events and camera rays. Milestone 8.39j, and the FOURTEENTH hero to take the permanent
    /// player-path gate - the last block of her series.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals and she is not among them, so every
    /// arm that deals her restages both heroes into the engagement milestone 8.15 built - exactly as
    /// the thirteen fixtures before this one do.</para>
    ///
    /// <para><b>She needs no ally arm</b>, which milestone 8.39i's staging remarks already state from
    /// the other side: nothing her eighteen dealt faces select names a FRIENDLY hero. Her Red line
    /// takes units adjacent or in range, her Blue line hands a modal to an enemy hero, her two Swaps
    /// exchange her with an enemy MINION and her three Green charges qualify their destination by an
    /// enemy hero beside it.</para>
    ///
    /// <para><b>THE STAGED BOARD WAS MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, which is the
    /// lesson milestone 8.38f records and the reason the offers below are asserted as exact sets.
    /// The engagement stands her at (-3,1) and the one enemy hero at (-2,2) on a deliberately BENT
    /// line, with a friendly Blue melee minion at (-3,2), an enemy Red ranged minion at (-4,2), enemy
    /// Red melee minions at (-1,-1) and (-1,0), and terrain at (-3,3), (-4,3) and (-3,-2). Three of
    /// her faces have NO legal play on that board untouched - the Gold basic's push needs an ADJACENT
    /// enemy hero, and her Blue and Green lines need an enemy hero standing beside another enemy unit
    /// - so those journeys restage through
    /// <see cref="MatchSceneCompositionRoot.NextComposedSandboxStaging"/>, which is the Director's own
    /// sandbox and the seam Silverarrow's and Tali's journeys already use for exactly this.</para>
    /// </summary>
    public sealed class CutterUiJourneyPlayModeTests
    {
        /// <summary>
        /// The staged seat dealt Cutter's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState CutterSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == CutterCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Cutter to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the two heroes where this journey needs them.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the
        /// ids only exist once a configuration has been composed - which is precisely the shape
        /// milestone 8.37a's Guardian Spirit journey uses. Nothing else about the scene changes: hands
        /// are left as the staging dealt them, and a null coordinate leaves a hero where setup put
        /// them.</para>
        /// </summary>
        internal static IEnumerator RestagedScene(
            SceneCardContent content,
            HexCoordinate? cutterAt = null,
            HexCoordinate? enemyAt = null,
            int? cutterCoins = null)
        {
            yield return LoadStagedScene(content);
            if (cutterAt == null && enemyAt == null && cutterCoins == null)
            {
                yield break;
            }

            var surveyed = Root().Runtime;
            var cutterId = CutterSeat(surveyed).HeroId;
            var enemyId = OtherHeroId(surveyed, cutterId);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(cutterId, cutterAt, coins: cutterCoins),
                    new SandboxHeroStaging(enemyId, enemyAt)
                });
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// Drives her named card to the point where its primary action is hers to submit, and returns
        /// with the turn standing on her. Every content journey of hers opens this way.
        /// </summary>
        internal static IEnumerator HerTurnOn(
            MatchSceneCompositionRoot root, HeroId cutterId, CardDefinitionId definitionId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, cutterId, definitionId);
            yield return YieldUntilTheNamedSeatActs(runtime, cutterId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(cutterId),
                "Her card has to open the turn for this journey to drive it. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the effect control whose own label the caller names, then presses the confirm that
        /// stands behind it when one does.
        ///
        /// <para><b>Selecting is not taking</b>, which milestone 8.35z53's first Play Mode run
        /// recorded: a <c>Choose</c> stands on Deselect/Confirm until the electing seat commits, and
        /// that is the ordinary shape of every button-answered choice in this build. The confirm is
        /// looked up again after the select rather than remembered, because the model is rebuilt.</para>
        /// </summary>
        /// <remarks>
        /// <b>Milestone 8.46c: both halves read the DECISION WINDOW's own model</b>, for the reason
        /// <c>BainUiJourneyPlayModeTests</c>'s own copy states - the controls moved there, and
        /// milestone 8.46a is replacing every one of their labels with the consequence the content
        /// declares.
        /// </remarks>
        internal static IEnumerator PressEffectControl(
            MatchSceneCompositionRoot root, string label)
        {
            var runtime = root.Runtime;
            var standing = runtime.DecisionWindowView.Model?.DecisionKey;
            UiPlayer.Click(UiPlayer.Button(root.Hud, label));
            yield return null;
            yield return null;

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
        /// definition id of the face that was clicked.
        /// </summary>
        internal static IEnumerator ClickACardFace(
            MatchSceneCompositionRoot root, System.Action<string> clicked = null)
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
            clicked?.Invoke(choices[0].Card.DisplayName);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", faceName));
            yield return null;
            yield return null;
        }

        internal static int HandCountOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards.Count(card =>
                card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand);

        internal static string ZoneOf(ConfiguredMatchSceneRuntime runtime, HexCoordinate space)
        {
            var map = runtime.ReadModel.SelectedMap;
            var found = map.Spaces.FirstOrDefault(value => value.Coordinate.Equals(space));
            Assert.That(found, Is.Not.Null, $"The map has no space at {space}.");
            return found.ZoneId.HasValue ? found.ZoneId.Value.Value : "terrain";
        }

        // -------------------------------------------------------------------------------------------
        // The component half her piece owns, on her own dealt board
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HerTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;

            // The highest-Initiative card of hers printing a boot. Grappling Bolt prints NO boot at
            // all - its one ribbon icon is a Defense shield drawn in the boot's slot, which is
            // milestone 8.35z20's slot-position trap and the hardest single reading of her inventory
            // pass - so the helper choosing by printed value is what keeps this journey off it.
            yield return CommitEverySeat(root, cutterId, MovingCardOf(runtime, cutterId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(cutterId));
            Assert.That(runtime.GetCommitmentStatus(cutterId),
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

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(destination),
                "Her piece follows her authoritative space after a walk a player drove.");

            if (runtime.PendingMovementChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }

            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(cutterId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        /// <summary>
        /// p.14's Defense response and p.13's Defeat and respawn, on her own piece.
        ///
        /// <para><b>The enemy is STOOD beside her rather than walked there</b>, which is milestone
        /// 8.37a's own correction of an arc that cost turns for nothing: the engagement is two spaces
        /// wide, an Attack needs contact, and a gap-closing turn proves nothing this journey is about.
        /// The sandbox stands him on (-2,1), the one free space adjacent to her that the board probe
        /// named.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerDefenseAnswersAnAttack_AndHerPieceRespawnsWhenItDoesNot()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedCutter,
                enemyAt: new HexCoordinate(-2, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(
                HexDistanceQuery.Calculate(
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate),
                Is.EqualTo(1),
                "The whole arc depends on his Attack needing no turn to reach her.");

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, cutterId,
                LowestInitiativeIdOf(runtime, cutterId, CutterCardContent.GrapplingBolt));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before her own card. " + Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(cutterId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;

            // p.14's Defense response standing IS the SecondaryDefenseOffer shape every one of her
            // rows rests here: SEVENTEEN of her nineteen faces print a Defense secondary - every face
            // but the Ultimate and, on the other side of the trap, Grappling Bolt prints ONLY that -
            // so the offer is what a player actually reads at this moment.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers, Is.Not.Empty,
                "Her hand holds Defense secondaries, so the offer is not empty.");
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat regardless of either printed value.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(cutterId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(cutterId)), Is.False);

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, cutterId, CutterCardContent.GrapplingBolt);
            yield return YieldUntilTheNamedSeatActs(runtime, cutterId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(cutterId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Cutter must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(cutterId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Cutter token stands after the respawn, not a second beside a stale one.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Her five starting-hand faces
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Grappling Bolt: "Target an obstacle in range and in a straight line, with no obstacles
        /// between you; ignore immunity. Move in a straight line towards that obstacle until you are
        /// adjacent."
        ///
        /// <para><b>The only face in the corpus that targets an OBSTACLE, and the offer is the whole
        /// of what makes that a rule rather than a word.</b> p.18 admits at most one obstacle in any
        /// one space - "Ever. No exceptions." - so the target is named by where it stands, and "with
        /// no obstacles between you" means each of p.19's six files offers AT MOST ONE candidate: the
        /// first obstacle along it. Measured on this board, that is exactly four - the Red ranged
        /// minion at (-4,2) and the friendly Blue melee minion at (-3,2), both adjacent; the Red melee
        /// minion at (-1,-1) two along the third file; and TERRAIN at (-3,-2) three along the first.
        /// </para>
        ///
        /// <para><b>The terrain space is the one this journey takes</b>, because terrain is the case a
        /// unit-shaped reading of the sentence would have missed entirely, and because it is three
        /// away: the grapple has to walk her two spaces to (-3,-1) and stop beside it, which is the
        /// printed "until you are adjacent" doing visible work.</para>
        ///
        /// <para>And the offer's NEGATIVE half is asserted with it: (-3,3) is terrain on the same file
        /// as the friendly minion at (-3,2) and is NOT offered, because the minion is between them.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator GrapplingBoltReelsHerToTheFirstObstacleOnAFile_AndStopsBesideIt()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedCutter);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;

            yield return HerTurnOn(root, cutterId, CutterCardContent.GrapplingBolt);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Her Silver basic asks which obstacle. " + Remaining(runtime));
            var offered = OfferedSpaces(runtime);
            var shape = string.Join(", ", offered.Select(value =>
                value + "/d" + HexDistanceQuery.Calculate(from, value)));

            Assert.That(offered, Is.EqualTo(new[]
                {
                    new HexCoordinate(-4, 2), new HexCoordinate(-3, -2),
                    new HexCoordinate(-3, 2), new HexCoordinate(-1, -1)
                }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "Each of p.19's six files offers at most the FIRST obstacle along it, and nothing " +
                "behind one. Offer was: " + shape);
            Assert.That(offered, Has.No.Member(new HexCoordinate(-3, 3)),
                "The terrain two along the file a minion already blocks is not offered, which is " +
                "what \"with no obstacles between you\" means. Offer was: " + shape);
            foreach (var candidate in offered)
            {
                Assert.That(StraightLineQuery.AreInStraightLine(from, candidate), Is.True,
                    "Every offered obstacle stands on one of p.19's straight lines out of her own " +
                    "space. Offer was: " + shape);
                Assert.That(HexDistanceQuery.Calculate(from, candidate), Is.InRange(1, 5),
                    "and inside the printed Range 5. Offer was: " + shape);
            }

            var terrain = new HexCoordinate(-3, -2);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(terrain).transform.position), Is.True,
                "The obstacle is named by clicking the space it stands on. " +
                UiPlayer.DescribeBoardRay(pointerInput, Tile(terrain).transform.position));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(new HexCoordinate(-3, -1)),
                "\"Move in a straight line towards that obstacle until you are adjacent\" walked her " +
                "the two spaces between and stopped her beside terrain rather than on it. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The face is finished: the Move names its own destination and asks nothing. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Walk the Plank: "Choose one — • Push an enemy hero adjacent to you up to 4 spaces; if that
        /// hero is pushed into another zone, that hero discards a card, or is defeated. • Defeat a
        /// minion adjacent to you."
        ///
        /// <para><b>The zone crossing is the whole point and the board had to be arranged for it.</b>
        /// The engagement's two heroes stand two apart, which the printed "adjacent to you" refuses
        /// outright, and p.18 pushes directly away from the pusher - so this journey stands her on
        /// (-2,1) with the enemy on (-2,2), where the push line runs (-2,3), (-2,4), (-2,5) and that
        /// last space is the first one in the Blue approach zone. Three spaces of the printed four.
        /// </para>
        ///
        /// <para><b>The distance is a real control, and finding that is what corrected this gate's own
        /// derivation.</b> `synthetic-push-target@3` reads the same `distance`/`leastDistance` pair
        /// the batch push does and asks the player when they differ; every face naming it before this
        /// one declared one distance, so the question had never been asked and the matrix keyed its
        /// `PushDistanceChoice` on the batch id alone. Measured here: the offer is the five integers
        /// 0, 1, 2, 3 and 4.</para>
        ///
        /// <para><b>A snapshot is taken and restored INSIDE the resolution</b>, which is the arm
        /// milestone 8.39h recorded it could not reach because no configured match dealt her. The
        /// pause the restore lands on is the same node the capture was taken at, and the journey then
        /// answers it on the same surface.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator WalkThePlankPushesAHeroOutOfTheBattleZone_AndTakesACardForIt()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedCutter,
                cutterAt: new HexCoordinate(-2, 1),
                enemyAt: new HexCoordinate(-2, 2));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var pushedTo = new HexCoordinate(-2, 5);

            Assert.That(ZoneOf(runtime, new HexCoordinate(-2, 2)),
                Is.Not.EqualTo(ZoneOf(runtime, pushedTo)),
                "The arc rests on those two spaces being in different zones, so it is asserted " +
                "before anything is driven.");

            yield return HerTurnOn(root, cutterId, CutterCardContent.WalkThePlank);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            // p.10's modal. BOTH bullets stand: the enemy hero is adjacent and so is the Red ranged
            // minion at (-4,2)... which is two away from (-2,1), so the minion bullet's subject here
            // is whichever enemy minion her staged space touches. Asserting the COUNT is the shape.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Her Gold basic opens p.10's modal. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates.Count, Is.EqualTo(2),
                "\"Choose one —\" prints two bullets and both reach this board. " +
                Remaining(runtime));
            yield return PressEffectControl(root, BulletLabelFor(runtime, "plank-push-arm"));

            // The subject: an enemy hero adjacent to her, named by clicking the piece.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("plank-victim"),
                "The push arm asks whom first. " + Remaining(runtime));
            Assert.That(
                runtime.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "\"an enemy hero adjacent to you\" names the one hero standing beside her.");

            // SNAPSHOT RESTORE, mid-resolution and on the pause a player is looking at.
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The pushed hero is named by clicking the piece where they stand.");
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("plank-push"),
                "and the answer moved the resolution on to the distance. " + Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The payload taken at the standing pause restores.");
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("plank-victim"),
                "and it is the SAME question the capture was taken on, answerable on the same " +
                "surface. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "and the restored question takes the same click it took before.");
            yield return null;
            yield return null;

            // The distance, which is a control this gate did not know was drawn until it was measured.
            var distance = runtime.PendingEffectChoice;
            Assert.That(distance, Is.Not.Null, Remaining(runtime));
            Assert.That(distance.NodeId.Value, Is.EqualTo("plank-push"), Remaining(runtime));
            Assert.That(
                distance.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Integer)
                    .Select(candidate => candidate.Value.IntegerValue)
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4 }).AsCollection,
                "\"up to 4 spaces\" draws every legal distance including the nought p.18 admits. " +
                Remaining(runtime));

            var three = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Integer &&
                value.Command.EffectChoice.Candidate.Value.IntegerValue == 3);
            yield return PressEffectControl(root, three.Label);

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(pushedTo),
                "Three spaces directly away from her is where p.18 puts him. " + Remaining(runtime));

            // "if that hero is pushed into another zone, that hero discards a card" - and the discard
            // is THEIRS to choose, which is the seam milestone 7.1a built and this card reaches from
            // the pusher's own turn.
            var handBefore = HandCountOf(runtime, enemyId);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Crossing a zone boundary is what opens the discard. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "The hero who was pushed picks which card they lose. " + Remaining(runtime));
            yield return ClickACardFace(root);

            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1),
                "The hero carried out of the battle zone is one card lighter. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The step completed by its discard, so the escalation to a Defeat never runs. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Daring Strike: "Choose one — • Before the attack: Move 1 space in a straight line. Target a
        /// hero adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent to
        /// you."
        ///
        /// <para><b>The directional narrowing is what this journey measures.</b> Milestone 8.39a built
        /// it as a narrowing of the radius-1 selection Brogan's charges ask AFTER their Move - one
        /// space where his admits six - so the proof is that the target offer holds exactly ONE
        /// candidate and that it is the hero standing along the heading she just travelled.</para>
        ///
        /// <para><b>The board had to be arranged and the arithmetic says why.</b> The first bullet can
        /// only reach a hero at C + 2d for a hex direction d, so the enemy has to be exactly two away
        /// ON a line with the space between him and her free. The engagement's own geometry is a BENT
        /// line - she is at (-3,1), he is at (-2,2) - which no distance of the Move can fix, on any of
        /// the three lunges. The sandbox stands him at (-1,1), which the board probe named as free,
        /// and (-2,1) between them is free too.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DaringStrikeMovesFirst_AndReachesOnlyTheHeroInTheDirectionOfThatMove()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedCutter,
                enemyAt: new HexCoordinate(-1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var step = new HexCoordinate(-2, 1);

            yield return HerTurnOn(root, cutterId, CutterCardContent.DaringStrike);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            // p.14's pause BEFORE the attack, drawn as p.10's modal: the card reaches step 1 with no
            // target at all and each bullet binds one.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Her melee Red stops the page before the attack. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates.Count, Is.EqualTo(2), Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "p.14 has not asked whom yet, because this card's own text names the target.");
            yield return PressEffectControl(root, BulletLabelFor(runtime, "lunge-move-arm"));

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("lunge-line"),
                "The taken bullet asks where the one printed space goes. " + Remaining(runtime));
            var landings = OfferedSpaces(runtime);
            Assert.That(landings, Is.EqualTo(new[]
                {
                    new HexCoordinate(-4, 1), new HexCoordinate(-3, 0),
                    new HexCoordinate(-2, 0), new HexCoordinate(-2, 1)
                }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "\"Move 1 space in a straight line\" offers the four free neighbours of hers and " +
                "neither of the two a minion stands on. Offer was: " +
                string.Join(", ", landings.Select(value => value.ToString())));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(step).transform.position), Is.True,
                "The lunge's own step is answered on the board.");
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(step),
                "She moved before the attack, which is the half that raises it. " + Remaining(runtime));

            var victim = runtime.PendingEffectChoice;
            Assert.That(victim, Is.Not.Null,
                "and the sentence then names whom. " + Remaining(runtime));
            Assert.That(victim.NodeId.Value, Is.EqualTo("lunge-victim"), Remaining(runtime));
            Assert.That(
                victim.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "\"in the direction of the move\" leaves ONE of the six spaces a radius-1 selection " +
                "would admit, and the hero standing on it is the target. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The named hero is taken by clicking the piece where they stand.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "p.14 then runs the attack the card text bound, so the defender is asked. " +
                Remaining(runtime));
            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }
        }

        /// <summary>
        /// Bombardment: "An enemy hero adjacent to another enemy unit and not adjacent to you discards
        /// a card, if able."
        ///
        /// <para><b>Two narrowings measured from two different places, and the untouched engagement
        /// satisfies neither.</b> The enemy hero starts two from her with no Red unit beside him at
        /// all, so the subject set is empty and the face would prove only that an empty offer is
        /// empty. The sandbox stands him at (-1,1), which the board probe named: beside the Red melee
        /// minion at (-1,0), two from her - inside the printed Radius 3 and outside p.9's innermost
        /// ring, which "not adjacent to you" drops.</para>
        ///
        /// <para><b>And the discard is answered from the SUBJECT's own seat</b>, which is the half a
        /// journey answering from her side would not be exercising at all.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BombardmentReachesTheEnemyBesideAnotherEnemy_AndTheyPickTheirOwnDiscard()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedCutter,
                enemyAt: new HexCoordinate(-1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var her = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            var him = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;

            Assert.That(HexDistanceQuery.Calculate(her, him), Is.EqualTo(2),
                "\"not adjacent to you\" needs him off her innermost ring and inside Radius 3.");
            Assert.That(
                runtime.ReadModel.Minions.Any(minion =>
                    minion.TeamSide == TeamSideOf(runtime, enemyId) &&
                    HexDistanceQuery.Calculate(minion.Coordinate, him) == 1),
                Is.True,
                "and \"adjacent to another enemy unit\" needs one of his own beside him.");

            yield return HerTurnOn(root, cutterId, CutterCardContent.Bombardment);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null,
                "With both clauses satisfied the Skill has a subject. " + Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("bombard-subject"), Remaining(runtime));
            Assert.That(subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "The one hero both clauses admit. " + Remaining(runtime));

            var handBefore = HandCountOf(runtime, enemyId);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null,
                "\"discards a card\" opens on the subject's own hand. " + Remaining(runtime));
            Assert.That(discard.ChooserId, Is.EqualTo(enemyId),
                "and it is THEIR choice which card goes - a prompt answered from her seat would be " +
                "a different rule from the one printed. " + Remaining(runtime));
            Assert.That(discard.ChooserId, Is.Not.EqualTo(cutterId));
            yield return ClickACardFace(root);

            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1),
                "Exactly the clicked face left their hand. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        /// <summary>
        /// Brace for Impact: "Move 3 spaces in a straight line, ignoring obstacles, to a space adjacent
        /// to an enemy hero; that hero discards a card, if able."
        ///
        /// <para><b>The offer is ONE space, and that single number is the whole clause working.</b>
        /// Exactly three, in a straight line, and adjacent to an enemy hero - on this board the only
        /// space satisfying all three is (0,1), three along the r=1 file. Its route runs through
        /// (-1,1), where the enemy hero himself is standing, so the charge CROSSES a piece and lands
        /// beyond it: that is "ignoring obstacles" doing something a p.19 walk could not, and p.18's
        /// second sentence still forbids stopping on one.</para>
        ///
        /// <para><b>"That hero" is named AFTER the Move</b>, by a separate radius-1 selection - the
        /// correction milestone 8.39e made to this series' own audit row, and the shape milestone
        /// 8.35p established for every charge in the build.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BraceForImpactChargesThroughAHero_AndTakesACardFromWhoItLandsBeside()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedEngagedStagedCutter,
                enemyAt: new HexCoordinate(-1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var cutterId = CutterSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, cutterId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate;
            var landing = new HexCoordinate(0, 1);

            yield return HerTurnOn(root, cutterId, CutterCardContent.BraceForImpact);
            yield return PointTrayAt(root, cutterId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The charge asks where it ends. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("charge-destination"),
                Remaining(runtime));
            var destinations = OfferedSpaces(runtime);
            Assert.That(destinations, Is.EqualTo(new[] { landing }).AsCollection,
                "Exactly three spaces, on a line, ending beside an enemy hero - one space on this " +
                "board answers all three. Offer was: " +
                string.Join(", ", destinations.Select(value => value.ToString())));
            Assert.That(HexDistanceQuery.Calculate(from, landing), Is.EqualTo(3));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(landing).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(cutterId)).Coordinate,
                Is.EqualTo(landing),
                "She crossed the space the enemy hero occupies and landed beyond him, which is what " +
                "\"ignoring obstacles\" permits. " + Remaining(runtime));

            var victim = runtime.PendingEffectChoice;
            Assert.That(victim, Is.Not.Null,
                "\"that hero\" is asked after the Move, not inside the charge line. " +
                Remaining(runtime));
            Assert.That(victim.NodeId.Value, Is.EqualTo("charge-victim"), Remaining(runtime));
            Assert.That(victim.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection);

            var handBefore = HandCountOf(runtime, enemyId);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId));
            yield return ClickACardFace(root);
            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1),
                "The hero she landed beside is one card lighter. " + Remaining(runtime));
        }

        /// <summary>The team side a hero is on, for a clause about that hero's OWN side.</summary>
        internal static GuardsDigital.Core.Participants.TeamSide TeamSideOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(heroId)).TeamSide;
    }
}

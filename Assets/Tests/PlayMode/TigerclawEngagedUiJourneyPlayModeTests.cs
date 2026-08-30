using System.Collections;
using System.Collections.Generic;
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
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TigerclawUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tigerclaw's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Tigerclaw two spaces apart, with an enemy Ranged minion beside him and a friendly
    /// Melee minion beside them both. Milestone 8.23.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts him where a printed sentence can reach, the enemy's own turn, the enemy's
    /// own Attack - ride the headless command surface, exactly as milestones 8.15, 8.20a, 8.21 and
    /// 8.22 do, and are marked where they appear.</para>
    ///
    /// <para><b>One turn is one action, and that is what shapes these journeys.</b> Spending the
    /// secondary Movement ends the turn - the stop control and the end-turn control are the same
    /// command - so a face whose printed sentence needs him somewhere else spends a previous turn
    /// walking there. Light-Fingered is the exception and it is the card's own doing: it prints
    /// "You may move 1 space" ahead of the take, so the step that brings him into contact is part of
    /// the sentence rather than a previous turn.</para>
    ///
    /// <para><b>No staged enemy hand holds a ranged Attack</b>, at any of p.15's stages. So the three
    /// Blocks of his that name one - Dodge, Sidestep and Evade - can be met on the surface as an
    /// offer and as a rider, and their printed condition is answered authoritatively by
    /// <c>TigerclawVerifiedContentTests.ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne</c>. What
    /// the Dodge journey below proves instead is the transition milestone 8.20b separated
    /// <see cref="PlayerInteractionShape.BlockDefenseOffer"/> out to capture: a Block answered against
    /// an attack its condition does not name defeats the hero who answered with it.</para>
    /// </summary>
    public sealed class TigerclawEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// The space every Defense journey walks him to: adjacent to the enemy hero, and the only
        /// such space on this staging with no enemy minion beside it.
        ///
        /// <para>It has to be that one. p.14 composes the minion contributions standing around a
        /// defender into their Defense Total, and his printed values are close enough to the enemy's
        /// Attack Totals that one adjacent enemy minion is the whole difference between a face that
        /// answers and a face that does not - Backstab's printed 5 answers Electrocute's 5 from here
        /// and is one short from anywhere else beside them.</para>
        /// </summary>
        internal static readonly HexCoordinate DefenceContact = new HexCoordinate(-1, 2);

        /// <summary>
        /// Blink Strike: "Before the attack: Move 2 spaces in a straight line through an enemy unit;
        /// target that unit. ( If you cannot make this move, you cannot attack. )"
        ///
        /// <para>One board click answers the whole first sentence. The line is what the card asks
        /// about - which enemy to cross - and the space he lands on and the unit he attacks both fall
        /// out of it, so a player picks a miniature and watches him appear on the far side of it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BlinkStrike_CrossesTheEnemyItsBoardClickNamesAndAttacksThatUnit()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                "Blink Strike's printed Initiative 13 acts before the lowest card the enemy holds.");
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // The card's own step-1 line is asked BEFORE p.14's target question, because it is what
            // names the target: a player never answers a separate "who do you attack".
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The started Attack must stop on its printed line. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and it stops there instead of asking p.14's own target question, which this card " +
                "answers for itself.");
            var offered = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(offered, Has.Length.EqualTo(1),
                "One enemy stands a single space away in a straight line with the space beyond it " +
                "empty, so one miniature is offered. " + Remaining(runtime));
            var crossed = offered[0].Occupancy.MinionId;
            var crossedAt = offered[0].Occupancy.Coordinate;
            var line = StandingNode(runtime);

            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, offered[0].transform.position), Is.True,
                "The line is answered by clicking the unit he crosses.");
            yield return null;
            yield return ConfirmIfStillStanding(root, line);
            yield return null;

            var landedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;
            Assert.That(landedAt, Is.Not.EqualTo(startedAt));
            Assert.That(HexDistanceQuery.Calculate(landedAt, startedAt), Is.EqualTo(2),
                "\"Move 2 spaces in a straight line\" - p.18's direct Move takes him exactly the two " +
                "the line published.");
            Assert.That(HexDistanceQuery.Calculate(landedAt, crossedAt), Is.EqualTo(1),
                "and it takes him THROUGH the unit rather than around it: he lands one space beyond " +
                "the piece he crossed, which is the far side of it.");
            Assert.That(HeroView(tigerclawId).Coordinate, Is.EqualTo(landedAt),
                "The token followed him there.");
            Assert.That(
                Object.FindObjectsByType<StandardMinionView>().Any(view =>
                    view.Occupancy != null && view.Occupancy.MinionId.Equals(crossed)),
                Is.False,
                "\"target that unit\" - the Attack took the piece the line crossed, so its " +
                "miniature has left the board.");

            yield return CaptureGameView("blink-strike-lands-beyond-the-unit-it-crossed");
        }

        /// <summary>
        /// Hit and Run: "Target a unit adjacent to you. After the attack: You may move 1 space."
        ///
        /// <para>p.14's step 7, answered on the board. The Attack's own target is a board click too,
        /// and what makes the rider worth its own journey is that it stands after the piece it
        /// targeted has already been removed - so the space that piece was standing on is one a
        /// player can step into.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HitAndRun_AttacksAndThenTakesItsPrintedStepOnItsOwnControls()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.HitAndRun);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId));
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate;

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));
            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty,
                "An enemy minion stands beside him on the staged board.");
            var targetView = MinionView(targets[0]);
            var targetAt = targetView.Occupancy.Coordinate;
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, targetView.transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack: You may move 1 space\" is p.14's step 7 and it asks. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty, "The step must offer somewhere to take it.");
            Assert.That(destinations, Has.Member(targetAt),
                "including the space the Attack just emptied - step 7 runs after step 6 removed " +
                "what it targeted, so that space is one he may step into.");
            var step = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, step);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(chosen));
            Assert.That(chosen, Is.Not.EqualTo(startedAt), "He took the step rather than standing.");
            Assert.That(HexDistanceQuery.Calculate(chosen, startedAt), Is.EqualTo(1),
                "One space is exactly one: the distance is not `up to`.");
            Assert.That(HeroView(tigerclawId).Coordinate, Is.EqualTo(chosen));
        }

        /// <summary>
        /// Blend Into Shadows: "If you are adjacent to terrain, place yourself into a space in radius;
        /// you cannot be targeted until the end of your next turn."
        ///
        /// <para>Two readings and one input. The input is the space he places himself into, clicked
        /// inside the printed Radius 2. The readings are the Active effect the placement raises -
        /// drawn on his own tray as the played card wearing the outline - and that same effect still
        /// standing on a later turn, where a player meets it with nothing on screen having just
        /// happened.</para>
        ///
        /// <para>The walk that puts him beside terrain is setup and rides the headless surface. The
        /// map prints terrain at (-6, 2); (-5, 2) is the nearest playable space beside it and his
        /// printed Movement reaches it in two.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BlendIntoShadows_PlacesHimInsideItsRadiusAndDrawsTheActiveEffectItRaises()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var besideTerrain = new HexCoordinate(-5, 2);

            yield return WalkTo(root, tigerclawId, TigerclawCardContent.HitAndRun, besideTerrain);
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(besideTerrain),
                "The setup walk has to leave him beside the printed terrain, or the card's own " +
                "condition is false and this journey proves the other branch.");

            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlendIntoShadows);
            // His Blue basic prints the lowest Initiative he holds after the walk, so the enemy's
            // own turn comes first inside the same round and is yielded rather than asserted away.
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            var placedCardId = runtime.SelectedActor.CardInstanceId;

            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Standing beside terrain makes the condition true, so the Place asks where. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(
                destinations.All(space =>
                    HexDistanceQuery.Calculate(space, besideTerrain) <= 2), Is.True,
                "The disk offered is the printed Radius 2 measured from him.");
            var place = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, place);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(chosen),
                "p.18's Place puts him into the space he clicked.");
            Assert.That(HeroView(tigerclawId).Coordinate, Is.EqualTo(chosen),
                "and the token is there rather than where he vanished from.");

            // p.19's `Next Turn` is what decides when the reading exists. The effect is CREATED by the
            // Place and becomes applicable at his own next TurnStarted, and a dormant instance is
            // deliberately not reported as live - so a player meets this one on a later turn, with
            // nothing on screen having just happened, which is what makes it a standing reading rather
            // than a consequence of the click.
            Assert.That(runtime.IsActiveEffectSource(placedCardId), Is.False,
                "\"until the end of your NEXT turn\" has not started yet, and a dormant instance is " +
                "not something to draw as live.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);

            Assert.That(runtime.IsActiveEffectSource(placedCardId), Is.True,
                "At his own next turn the shadow is in effect, and the card he played is its live " +
                "source. " + Remaining(runtime));

            // The player reads it on his own tray, as the played card's slot wearing the outline.
            yield return PointTrayAt(root, tigerclawId);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(
                hud, "gd-card-slot", "BLEND INTO SHADOWS");
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "and it wears the Active-effect outline while the shadow lasts.");

            yield return CaptureGameView("blend-into-shadows-places-him-and-draws-its-effect");
        }

        /// <summary>
        /// p.14's secondary Defense: the numeric icon every dealt face of his prints except his Blue
        /// basic, spent as a whole card face out of his own hand.
        ///
        /// <para><b>It is the Tier II staging because that is where a printed value of his can answer
        /// at all.</b> His Defense icons run from 1 to 6 and the enemy's Attack Totals are 5 and 6, so
        /// exactly two of his faces meet one - Backstab's 5 against Electrocute's 5 here, and the
        /// Ballista's 6 against Electroblast's 6 at p.15's next stage. Both are ties, and p.14 defends
        /// on a tie. From any other space beside the enemy an enemy minion stands adjacent to him and
        /// takes one off the total, which is the whole difference.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndTigerclawSurvivesTheAttack()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);

            yield return TakeTheEnemysAttack(
                root, tigerclawId, enemyId, TigerclawCardContent.PoisonedDagger,
                TigerclawCardContent.BlendIntoShadows, WaspCardContent.Electrocute);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.DefenseChoices
                .Where(choice => choice.DefenseBreakdown != null &&
                    choice.DefenseBreakdown.CardInstanceId.HasValue)
                .ToArray();
            Assert.That(
                offered.All(choice => OriginOfDefense(runtime, choice) ==
                    ActionDefinitionOrigin.Reaction ||
                    OriginOfDefense(runtime, choice) == ActionDefinitionOrigin.PrintedPrimary),
                Is.True,
                "Every offered face is either the secondary icon or one of his printed Blocks.");

            var numeric = offered.FirstOrDefault(choice =>
                choice.DefenseBreakdown.Outcome == AttackResolutionOutcome.SuccessfullyDefended &&
                OriginOfDefense(runtime, choice) == ActionDefinitionOrigin.Reaction);
            Assert.That(numeric, Is.Not.Null,
                "A numeric secondary Defense has to answer this Attack Total, or the journey proves " +
                "the Block arm instead. Shown: " + UiPlayer.Describe(hud));
            var faceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(numeric.DefenseBreakdown.CardInstanceId.Value))
                .DefinitionReference.DefinitionId);
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(tigerclawId) && card.State.Location == CardLocation.Hand);

            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(tigerclawId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore - 1),
                "A Defense is paid by discarding the card from hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(tigerclawId)), Is.True,
                "and the printed value answered the Attack, so he is still standing.");
        }

        /// <summary>
        /// Dodge is offered in p.14's prompt as a whole Block face, and answering with it against an
        /// attack its printed condition does not name defeats the hero who answered.
        ///
        /// <para>This is the transition milestone 8.20b separated
        /// <see cref="PlayerInteractionShape.BlockDefenseOffer"/> from the numeric Defense to capture:
        /// a Block is not compared, it succeeds outright or its hero is removed. "Block a ranged
        /// attack" is Dodge's whole condition and no staged enemy hand holds a ranged Attack at any
        /// stage, so the offer a player is shown is the honest one and so is what it costs them.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Dodge_IsOfferedAsAWholeBlockFace_AndItsUnmetConditionDefeatsHim()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);

            yield return TakeTheEnemysAttack(
                root, tigerclawId, enemyId, TigerclawCardContent.HitAndRun,
                TigerclawCardContent.BlendIntoShadows, WaspCardContent.Shock);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var block = runtime.RoundView.Model.DefenseChoices.FirstOrDefault(choice =>
                choice.DefenseBreakdown != null &&
                choice.DefenseBreakdown.CardInstanceId.HasValue &&
                DefinitionOf(runtime, choice.DefenseBreakdown.CardInstanceId.Value)
                    .Equals(TigerclawCardContent.Dodge));
            Assert.That(block, Is.Not.Null,
                "Dodge is in his hand and a Block is offered whether or not its condition is met - " +
                "which is what makes answering with it a decision. Shown: " + UiPlayer.Describe(hud));
            Assert.That(block.DefenseShineSlot, Is.EqualTo(CardShineSlot.PrimaryRing),
                "A primary Defense is invoked in the Reaction window, so its printed origin - not " +
                "its invocation context - must select Dodge's primary ring.");

            var faceName = NameOf(runtime, TigerclawCardContent.Dodge);
            var dodgeControl = UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName);
            UiPlayer.EnsureVisible(dodgeControl);
            yield return null;
            Assert.That(UiPlayer.Descendants(dodgeControl).OfType<UnityEngine.UIElements.Image>()
                    .Any(image => image.name == "gd-card-shine" &&
                        image.userData is CardShineSlot slot && slot == CardShineSlot.PrimaryRing),
                Is.True, "The live Dodge face carries its generated primary-ring overlay.");
            Assert.That(UiPlayer.Descendants(dodgeControl).OfType<UnityEngine.UIElements.Image>()
                    .Any(image => image.name == "gd-card-shine-primary-contrast"),
                Is.True, "and the contrast pass keeps that ring legible on the Blue face.");
            UiPlayer.Click(dodgeControl);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers the response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(tigerclawId)), Is.False,
                "\"Block a ranged attack\" - this attack is not one, so p.14's step 6 removes him " +
                "rather than comparing anything.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(tigerclawId)), Is.False,
                "and his token left the board with him.");
        }

        /// <summary>
        /// p.13's Defeat and respawn, with the piece as its subject: the token leaves the board when
        /// the answer is a decline, and comes back on a clicked spawn space at his own next turn. It
        /// is also the half of the spawn claim the engaged staging leaves open - the space he returns
        /// to is the map's own Blue hero spawn, not the space the staging moved him to.
        /// </summary>
        [UnityTest]
        public IEnumerator Tigerclaw_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return TakeTheEnemysAttack(
                root, tigerclawId, enemyId, TigerclawCardContent.HitAndRun,
                TigerclawCardContent.BlendIntoShadows, WaspCardContent.Shock);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndTigerclawSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(tigerclawId)), Is.False,
                "The defeated hero is off the board until he respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(tigerclawId)), Is.False,
                "and his token is gone with him rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            // p.13 offers the respawn at the defeated hero's own next turn, so the card he commits
            // for it has to take that turn: Blink Strike's printed 13 is above every card the enemy
            // still holds, and a lower one would leave the offer standing behind theirs.
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, tigerclawId, TigerclawCardContent.BlinkStrike);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                "The respawn is offered at his own turn, so his card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated hero must be offered his respawn at his own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(tigerclawId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is his own official token that came back.");
        }

        /// <summary>
        /// Light-Fingered: "You may move 1 space. Take 1 coin from an enemy hero adjacent to you; if
        /// you do, you may move 1 space."
        ///
        /// <para>Three printed steps and three inputs, and the first is what makes the second
        /// possible: the staged board leaves the enemy hero two spaces away, so the opening move is
        /// the step that brings him into contact. The take is the milestone's new control - a printed
        /// AMOUNT, pressed as its own number - and the coin it moves has to have been earned first,
        /// which the enemy's own turn does by defeating the minion standing beside them.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LightFingered_TakesTheEnemyHerosCoinOnItsOwnAmountControlAndThenSteps()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return TheEnemyEarnsACoin(root, tigerclawId, enemyId);
            Assert.That(CoinsOf(runtime, enemyId), Is.GreaterThan(0),
                "There is nothing to take until p.13's Defeat has paid the enemy something.");

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                "His own turn follows theirs inside the same round. " + Remaining(runtime));
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Step one: the printed opening move, taken towards the hero the take names.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You may move 1 space\" opens first. " + Remaining(runtime));
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var contact = LitDestinations(runtime)
                .Where(space => HexDistanceQuery.Calculate(space, enemyAt) == 1)
                .ToArray();
            Assert.That(contact, Is.Not.Empty,
                "One of the offered steps has to reach a space beside the enemy hero, or the take " +
                "behind it can never be about anybody. " + Remaining(runtime));
            var opening = StandingNode(runtime);
            var into = FirstClickableSpace(pointerInput, contact);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(into).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, opening);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(tigerclawId)).Coordinate,
                Is.EqualTo(into));

            // Step two: whose pile. One board click on the hero standing beside him.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"an enemy hero adjacent to you\" is the second question. " + Remaining(runtime));
            var victim = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The hero whose coins are taken is named by clicking them.");
            yield return null;
            yield return ConfirmIfStillStanding(root, victim);
            yield return null;

            // Step three: how many. The milestone's new control, drawn as the printed number.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Take 1 coin\" is answered on its own amount control. " + Remaining(runtime));
            var amounts = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(amounts, Is.Not.Empty,
                "The amount has to be drawn as something a player can press. Shown: " +
                UiPlayer.Describe(hud));
            Assert.That(amounts.Select(value => value.Label), Has.Member("1"),
                "and it is drawn as the printed number rather than as an anonymous option.");
            var takenFrom = CoinsOf(runtime, enemyId);
            var take = StandingNode(runtime);
            UiPlayer.Click(UiPlayer.Button(hud, "1"));
            yield return null;
            yield return ConfirmIfStillStanding(root, take);
            yield return null;

            Assert.That(CoinsOf(runtime, enemyId), Is.EqualTo(takenFrom - 1),
                "One coin left the hero it was taken from.");
            Assert.That(CoinsOf(runtime, tigerclawId), Is.EqualTo(1),
                "and arrived on his own pile: p.15's coins are transferred rather than minted.");

            yield return CaptureGameView("light-fingered-takes-the-coin-it-names");
        }

        /// <summary>
        /// The same amount, answered again after a retained-prefix Undo and after a snapshot restore.
        ///
        /// <para>The take is the pause worth this journey because it is the one decision of his that
        /// a player answers with a number: the amount control is re-derived from the pile it would
        /// move rather than remembered, so an Undo that gave back a control offering something else
        /// would be a restored match disagreeing with the one it came from.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheTheftsAmount_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTigerclaw);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var tigerclawId = TigerclawSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, tigerclawId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return TheEnemyEarnsACoin(root, tigerclawId, enemyId);
            yield return PointTrayAt(root, tigerclawId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return AnswerTheOpeningMoveAndTheVictim(root, tigerclawId, enemyId, pointerInput);

            Assert.That(StandingNode(runtime).Value, Is.EqualTo("theft-take"),
                "The journey needs the amount standing. " + Remaining(runtime));

            // Answer it once, so there is an answer to take back. One press on a select whose bounds
            // are exactly one is TWO accepted commands - the select, and the confirm the presenter
            // submits behind it - which is why giving the question back takes two presses of UNDO.
            var take = StandingNode(runtime);
            UiPlayer.Click(UiPlayer.Button(hud, "1"));
            yield return null;
            yield return ConfirmIfStillStanding(root, take);
            yield return null;
            Assert.That(CoinsOf(runtime, tigerclawId), Is.EqualTo(1),
                "The answered amount moved the coin. " + Remaining(runtime));

            // The Undo half first: a restore clears the accepted history, so there would be nothing
            // to undo afterwards.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            // The drawer relabels its own toggle when open - "DIRECTOR" becomes "DIRECTOR" plus a
            // chevron - so the label that opened it finds nothing when closing it.
            UiPlayer.Click(DrawerToggle(hud));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Undo gives a standing question back rather than the turn. " + Remaining(runtime));
            Assert.That(StandingNode(runtime).Value, Is.EqualTo("theft-take"),
                "and the question it gives back is the amount, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(CoinsOf(runtime, tigerclawId), Is.Zero,
                "with the coin back where it was taken from.");
            Assert.That(runtime.RoundView.Model.EffectChoices
                    .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                    .Select(value => value.Label),
                Has.Member("1"),
                "and the amount it offers is the one the pile can still pay, re-derived from that " +
                "pile rather than restored from the answer. Shown: " + UiPlayer.Describe(hud));

            // Now the restore half, from the live session standing on the same question.
            var captured = runtime.CaptureSnapshot();
            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The captured payload has to be readable by the build that wrote it.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is still standing on the same question. " + Remaining(runtime));
            Assert.That(StandingNode(runtime).Value, Is.EqualTo("theft-take"));
            var again = StandingNode(runtime);
            UiPlayer.Click(UiPlayer.Button(hud, "1"));
            yield return null;
            yield return ConfirmIfStillStanding(root, again);
            yield return null;

            Assert.That(CoinsOf(runtime, tigerclawId), Is.EqualTo(1),
                "and the answer given after the restore moves the coin the same way.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Turn one is the enemy's: they commit an Attack, defeat the minion standing beside them and
        /// are paid for it by rulebook p.13, and the staged hero's own turn follows inside the same
        /// round. It is entirely setup - the enemy's turn is somebody else's journey - so it rides the
        /// headless surface.
        /// </summary>
        internal static IEnumerator TheEnemyEarnsACoin(
            MatchSceneCompositionRoot root, HeroId tigerclawId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, tigerclawId, TigerclawCardContent.LightFingered);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Shock's 8 acts before Light-Fingered's 2.");

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var targets = runtime.PendingAttackTargetChoice;
            Assert.That(targets, Is.Not.Null, "The enemy's Attack opens p.14's target question.");
            var minion = targets.LegalTargetMinionIds.FirstOrDefault();
            Assert.That(minion.IsDefined, Is.True,
                "A minion has to be standing beside the enemy hero for their Attack to be paid for.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(minion)), Is.True);
            yield return null;
            yield return null;

            // The enemy Attack prints a rider of its own, and it is theirs to answer: a question left
            // standing on their turn is what stops the turn ending.
            for (var budget = 0; runtime.PendingEffectChoice != null && budget < 8; budget++)
            {
                var standing = runtime.RoundView.Model.EffectChoices.FirstOrDefault();
                if (standing != null)
                {
                    Assert.That(runtime.RoundPresenter.Submit(standing), Is.True);
                }
                else
                {
                    // An object-valued candidate comes through EffectChoiceTargets and is answered by
                    // the piece rather than by the space: the two are separate routes, and asking the
                    // wrong one makes a board-answered question look answerless.
                    var piece = runtime.RoundView.Model.EffectChoiceTargets.FirstOrDefault();
                    Assert.That(piece, Is.Not.Null, Remaining(runtime));
                    var heroes = runtime.Heroes
                        .Where(value => value.Coordinate.Equals(piece.Space))
                        .ToArray();
                    Assert.That(
                        heroes.Length == 1
                            ? runtime.BoardPresenter.SubmitHero(heroes[0].HeroId)
                            : runtime.BoardPresenter.SubmitDestination(piece.Space),
                        Is.True,
                        "The enemy's own rider has to be answerable. " + Remaining(runtime));
                }
                yield return null;
            }

            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
        }

        /// <summary>
        /// The two board clicks in front of the amount: the printed opening move into contact, and the
        /// hero whose pile the take names.
        /// </summary>
        private static IEnumerator AnswerTheOpeningMoveAndTheVictim(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            HeroId enemyId,
            PrimitiveHeroPointerInput pointerInput)
        {
            var runtime = root.Runtime;
            var hud = root.Hud;
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var contact = LitDestinations(runtime)
                .Where(space => HexDistanceQuery.Calculate(space, enemyAt) == 1)
                .ToArray();
            Assert.That(contact, Is.Not.Empty, Remaining(runtime));
            var opening = StandingNode(runtime);
            var into = FirstClickableSpace(pointerInput, contact);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(into).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, opening);
            yield return null;

            var victim = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, victim);
            yield return null;
        }

        /// <summary>
        /// Spends a previous turn walking him to the named space on the named card's secondary
        /// Movement, headlessly. The walk itself is
        /// <see cref="TigerclawUiJourneyPlayModeTests.Tigerclaw_CommitsFromHisOwnTrayAndWalksHisSecondaryMovementToAClickedSpace"/>'s
        /// subject; here it is setup, because one turn is one action and a card whose printed sentence
        /// needs him somewhere else cannot also be the card that takes him there.
        /// </summary>
        internal static IEnumerator WalkTo(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            CardDefinitionId walkOn,
            HexCoordinate destination)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, tigerclawId, walkOn);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(tigerclawId),
                walkOn.Value + " must act before the lowest card the enemy holds.");

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations
                    .Any(value => value.Destination.Equals(destination)),
                Is.True,
                "The staged board has to leave " + destination + " reachable on this card's printed " +
                "Movement, or the journey behind this walk is about a different space.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, "The enemy holds the rest of turn one.");
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Turn one puts the staged hero beside the enemy hero; turn two is the enemy's own adjacent
        /// Attack on him, stopped at p.14's Defense response. The card he commits for turn two decides
        /// what is left in his hand to answer with - and it is never the card the case answers with,
        /// because p.7 puts a committed card in the turn area where no Defense prompt can offer it.
        /// </summary>
        internal static IEnumerator TakeTheEnemysAttack(
            MatchSceneCompositionRoot root,
            HeroId tigerclawId,
            HeroId enemyId,
            CardDefinitionId walkOn,
            CardDefinitionId heCommits,
            CardDefinitionId enemyAttack)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, tigerclawId, walkOn);
            yield return YieldUntilTheNamedSeatActs(runtime, tigerclawId);
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations
                    .Any(value => value.Destination.Equals(DefenceContact)),
                Is.True,
                "The walk has to reach " + DefenceContact + " on " + walkOn.Value + "'s printed " +
                "Movement: it is the space beside the enemy hero with no enemy minion of theirs " +
                "beside it, and p.14's minion contributions are what decide whether any printed " +
                "Defense of his can answer at all.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(DefenceContact), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);

            Commit(runtime, enemyId, enemyAttack);
            Commit(runtime, tigerclawId, heCommits);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy Attack has to take the turn over the card he committed to keep his hand.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(tigerclawId),
                "The enemy he walked up to must have him as a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(tigerclawId)),
                Is.True);
            yield return null;
            yield return null;
        }

        /// <summary>The origin the offered Defense face is printed in - the secondary icon or a Block.</summary>
        internal static ActionDefinitionOrigin OriginOfDefense(
            ConfiguredMatchSceneRuntime runtime, TypedInvocationViewModel choice) =>
            runtime.Definitions
                .Single(definition => definition.Id.Equals(
                    DefinitionOf(runtime, choice.DefenseBreakdown.CardInstanceId.Value)))
                .ActionDefinitions
                .Single(action => action.Kind == ActionKind.Defense)
                .Origin;

        /// <summary>The definition a card instance was dealt from, for a prompt read by card rather than by label.</summary>
        internal static CardDefinitionId DefinitionOf(
            ConfiguredMatchSceneRuntime runtime, CardInstanceId cardInstanceId) =>
            runtime.Cards.Single(card => card.Id.Equals(cardInstanceId))
                .DefinitionReference.DefinitionId;

        /// <summary>The coins a hero's own rail is drawing, read through the projected ledger.</summary>
        internal static int CoinsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(heroId))
                .Coins;


        /// <summary>
        /// Answers p.14's Defense with the face the prompt itself says leaves the defender standing.
        /// The outcome is read off the breakdown the player is shown rather than off a printed number,
        /// so a change to minion contributions or to an Ultimate's standing rules moves the journey
        /// with the game instead of stranding it on an arithmetic assumption.
        /// </summary>
        internal static IEnumerator DefendSuccessfully(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "No offered face answers this Attack Total, so the defender would be defeated and " +
                "the rider under test would have no surviving target. " + Remaining(runtime));
            Assert.That(runtime.RoundPresenter.Submit(survives), Is.True);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// The offered Defense whose own preview says the defender survives it, or null when none does.
        /// </summary>
        internal static TypedInvocationViewModel SurvivingDefense(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.DefenseChoices.FirstOrDefault(choice =>
                choice.DefenseBreakdown != null &&
                choice.DefenseBreakdown.CardInstanceId.HasValue &&
                choice.DefenseBreakdown.Outcome == AttackResolutionOutcome.SuccessfullyDefended);

        /// <summary>The standing pause's own decline control, or null when it offers none.</summary>
        internal static TypedInvocationViewModel DeclineControl(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);

        internal static HeroId OtherHeroId(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(heroId)).HeroId;

        /// <summary>
        /// Commits the named card out of the named seat's hand and the lowest-Initiative card out of
        /// every other seat's, headlessly. The tray-clicked commitment has its own journey in
        /// <see cref="TigerclawUiJourneyPlayModeTests"/>, and rulebook p.7 resolves the higher printed
        /// Initiative first, so the other seat yielding is what makes the turn deterministic.
        /// </summary>
        internal static void CommitSoTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId actingHeroId, CardDefinitionId definitionId)
        {
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                if (seat.Equals(actingHeroId))
                {
                    Commit(runtime, seat, definitionId);
                    continue;
                }

                var lowest = runtime.Cards
                    .Where(card => card.OwnerId.Equals(seat) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => runtime.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .ThenBy(card => card.Id.Value, System.StringComparer.Ordinal)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(lowest.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }
        }

        /// <summary>
        /// Commits a named card for BOTH seats, for a setup turn that has to leave a particular card
        /// in the enemy's hand. Committing their lowest every turn spends their hand in ascending
        /// order, which takes the Attack a later turn needs.
        /// </summary>
        internal static void CommitBoth(
            ConfiguredMatchSceneRuntime runtime,
            HeroId actingHeroId,
            CardDefinitionId hisCard,
            HeroId enemyId,
            CardDefinitionId theirCard)
        {
            Commit(runtime, actingHeroId, hisCard);
            Commit(runtime, enemyId, theirCard);
        }

        internal static void Commit(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definitionId)
        {
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from {heroId.Value}'s hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>The selected actor's printed primary action, for a setup step that is not the subject.</summary>
        internal static ActionDefinitionId PrimaryActionOf(ConfiguredMatchSceneRuntime runtime) =>
            runtime.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable && OriginOf(runtime, candidate) ==
                    ActionDefinitionOrigin.PrintedPrimary).ActionDefinitionId;

        /// <summary>
        /// Ends whoever is acting until the actor is the named seat. A cross-team Initiative tie goes
        /// to the Tie Breaker side, which `DEV-005` fixes to Red, so a card of his that ties the
        /// enemy's lowest still reaches its own turn - after theirs.
        /// </summary>
        internal static IEnumerator YieldUntilTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            for (var budget = 0; budget < 4; budget++)
            {
                if (!runtime.SelectedActor.IsDefined ||
                    runtime.SelectedActor.ActorId.Equals(heroId))
                {
                    yield break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The enemy's turn is setup for this journey and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }

            Assert.Fail("The named seat never took a turn. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the confirm belonging to the node that was standing before the click, when one is
        /// still there. A selection of exactly one is confirmed by the presenter behind the click and
        /// a wider one is not, so the two are told apart by asking rather than by assuming.
        ///
        /// <para><b>The node has to be named, and that is what milestone 8.22's first Play Mode run
        /// found.</b> An unqualified "press whatever confirm is standing" pressed the <em>next</em>
        /// node's control: Troop Movement's printed repeat is a decision with a confirm and a decline
        /// on it, so confirming blindly after the destination click accepted the repeat instead of
        /// leaving it to be answered, and the journey met the second order's target question where it
        /// expected the printed choice.</para>
        /// </summary>
        internal static IEnumerator ConfirmIfStillStanding(
            MatchSceneCompositionRoot root, EffectNodeId node)
        {
            var runtime = root.Runtime;
            var standing = runtime.PendingEffectChoice;
            if (standing == null || !standing.NodeId.Equals(node))
            {
                yield break;
            }

            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm == null)
            {
                yield break;
            }

            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>The node a pause is standing on, for a confirm that must not reach past it.</summary>
        internal static EffectNodeId StandingNode(ConfiguredMatchSceneRuntime runtime)
        {
            var standing = runtime.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null,
                "No effect pause is standing to read a node from. " + Remaining(runtime));
            return standing.NodeId;
        }

        /// <summary>
        /// The Director drawer's own toggle, found by label PREFIX: the control relabels itself while
        /// the drawer is open, so the exact label that opened it closes nothing.
        /// </summary>
        internal static UnityEngine.UIElements.Button DrawerToggle(ConfiguredMatchHudRenderer hud)
        {
            var toggles = UiPlayer.Descendants(hud.RootElement)
                .OfType<UnityEngine.UIElements.Button>()
                .Where(candidate => UiPlayer.IsShown(candidate) &&
                    candidate.text != null &&
                    candidate.text.StartsWith("DIRECTOR", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(toggles, Has.Length.EqualTo(1),
                "Exactly one shown control opens the Director drawer; shown: " + UiPlayer.Describe(hud));
            return toggles[0];
        }

        /// <summary>Ends whoever is acting until nobody is, so the round can reach its own boundary.</summary>
        internal static IEnumerator YieldUntilNobodyIsActing(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.SelectedActor.IsDefined && budget < 6; budget++)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "Every remaining turn of this round is setup and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
                yield return null;
            }

            yield return null;
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

        internal static ActionDefinitionOrigin OriginOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Origin;

        internal static CardDefinitionId[] DiscardOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        /// <summary>
        /// The spaces a paused effect is offering, in board order. Space-valued candidates come through
        /// <c>EffectChoiceDestinations</c> and object-valued ones through <c>EffectChoiceTargets</c>;
        /// reading the wrong one is how a board-answered question looks answerless.
        /// </summary>
        internal static HexCoordinate[] LitDestinations(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

        /// <summary>The drawn miniatures standing on the given spaces, in the order the spaces come.</summary>
        internal static StandardMinionView[] MinionsAt(IEnumerable<HexCoordinate> spaces) =>
            spaces
                .Select(space => Object.FindObjectsByType<StandardMinionView>()
                    .FirstOrDefault(view => view.Occupancy != null &&
                        view.Occupancy.Coordinate.Equals(space)))
                .Where(view => view != null)
                .ToArray();

        /// <summary>
        /// What a paused frame is still asking, for a failure message that says what the player was
        /// actually looking at rather than only that something was left over.
        /// </summary>
        internal static string Remaining(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            return $"status='{model.Status}' choices=[" +
                string.Join(", ", model.EffectChoices.Select(value =>
                    $"{value.Command.EffectChoice.Kind}:{value.Label}")) +
                "] targets=[" +
                string.Join(", ", model.EffectChoiceTargets.Select(value => value.Space.ToString())) +
                "] destinations=[" +
                string.Join(", ", model.EffectChoiceDestinations.Select(value =>
                    value.Command.EffectChoice.Candidate.Value.Coordinate.ToString())) +
                "] defense=[" +
                string.Join(", ", model.DefenseChoices.Select(value => value.Label)) + "]";
        }

        /// <summary>Whether any hero or minion now stands on this space.</summary>
        internal static bool OccupantAt(ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Heroes.Any(hero => hero.Coordinate.Equals(space)) ||
            Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space));

        /// <summary>
        /// The first offered space whose own camera ray reaches the board rather than a hero piece
        /// standing over it. <c>PrimitiveHeroPointerInput</c> offers a ray to the hero selectors before
        /// the tiles, so a space drawn behind a piece is answered by clicking elsewhere on it.
        /// </summary>
        internal static HexCoordinate FirstClickableSpace(
            PrimitiveHeroPointerInput pointerInput, IReadOnlyList<HexCoordinate> spaces)
        {
            var camera = pointerInput.EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            foreach (var space in spaces)
            {
                var tile = Tile(space);
                var screen = camera.WorldToScreenPoint(tile.transform.position);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.Fail(
                "Every offered space is drawn behind a hero piece, so none of them can be clicked: " +
                string.Join(", ", spaces.Select(space => space.ToString())));
            return default;
        }

        /// <summary>
        /// One targeted Game View capture of what is on screen right now, for the non-batch pass the
        /// gate requires. A batch run skips it, so the same journeys serve both tiers.
        /// </summary>
        internal static IEnumerator CaptureGameView(string name)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/8-23-{name}.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(System.IO.File.Exists(path), Is.True,
                $"The targeted Game View evidence must be captured under tmp/ as {name}.png.");
        }

        internal static PrimitiveHeroView HeroView(HeroId heroId) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(heroId));

        internal static StandardMinionView MinionView(MinionId minionId) =>
            Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(minionId));

        internal static HexTileView Tile(HexCoordinate coordinate) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(coordinate));
    }
}

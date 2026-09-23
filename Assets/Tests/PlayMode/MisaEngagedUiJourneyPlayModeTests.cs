using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.MisaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Misa's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Misa two spaces apart, with an enemy Ranged minion beside her and a friendly Melee
    /// minion beside her too. Milestone 8.35z38.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the enemy's own turn, the enemy's own Attack - ride the headless command surface, exactly as
    /// milestones 8.15 through 8.35z34 do, and are marked where they appear.</para>
    ///
    /// <para><b>THE STAGED GEOMETRY IS HERO-INDEPENDENT and re-measured rather than assumed.</b>
    /// <c>EngagedMirrorStaging</c> restages whichever two heroes it is given onto the same pair of
    /// spaces, so the Blue seat stands at (-3, 1) and the Red seat at (-2, 2) whoever they are.
    /// Milestone 8.35z38 re-measured the whole board offline before writing these fixtures and
    /// confirmed it, together with the four answers her printed set actually turns on - see
    /// <see cref="MisaUiJourneyPlayModeTests"/>, which records them in full.</para>
    ///
    /// <para><b>What her kit CAN do on this board, and what it cannot.</b> An enemy Ranged minion
    /// stands adjacent to her, so her melee Reds have a subject without moving and Watch How I Soar's
    /// second bullet has a minion to defeat; twenty-two empty spaces stand at exactly Range 4, so its
    /// first bullet has somewhere to go; three straight lines of three or less cross a unit, so Sudden
    /// Breeze's rider has somebody to carry. What is NOT reachable here is the enemy HERO by a melee
    /// Attack - the staging holds the two two apart - and every journey below that wanted a hero
    /// target takes the adjacent minion instead and says so.</para>
    ///
    /// <para><b>Her Ultimate is locked on this staging, so her modal offers ONE bullet's worth of
    /// choice.</b> RULING 28's raise is the Tier III arm's subject and lives in
    /// <see cref="MisaProgressedUiJourneyPlayModeTests"/>; what these journeys prove is the printed
    /// card, which is the honest half to prove first.</para>
    /// </summary>
    public sealed class MisaEngagedUiJourneyPlayModeTests
    {

        /// <summary>
        /// Milestone 8.46e. p.10's bullets are the decision window's own option controls now - the
        /// strip no longer draws a family the window has taken - so they are counted here, and each
        /// is pressed by the branch node it stands for rather than by position or typed words.
        /// </summary>
        private static DecisionWindowControlViewModel[] OfferedBullets(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.DecisionWindowView.Model;
            return model == null
                ? new DecisionWindowControlViewModel[0]
                : model.Controls.Where(control =>
                    control.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    control.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    control.Command.EffectChoice.Candidate.OptionNodeId.IsDefined).ToArray();
        }

        [UnityTest]
        public IEnumerator WatchHowISoar_DefeatBulletRemovesTheClickedMinion()
        {
            UiPlayer.SetPlayerViewport(1920, 1080, "8.36f Misa");
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var misaId = MisaSeat(runtime).HeroId;
            yield return CommitEverySeat(root, misaId, NameOf(runtime, MisaCardContent.WatchHowISoar));
            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            // Milestone 8.46e: both bullets stand on the decision window; the Defeat arm is pressed by
            // the branch node it stands for rather than by its position.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2));
            UiPlayer.ClickEffectOption(runtime, root.Hud, "soar-defeat-arm");
            yield return null;
            yield return null;
            var at = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;
            var team = runtime.ReadModel.Participants.GetTeamForHero(misaId).Id;
            var minion = runtime.ReadModel.Minions.First(value => !value.TeamId.Equals(team) &&
                HexDistanceQuery.Calculate(at, value.Coordinate) == 1);
            yield return UiPlayer.CaptureGameView("8-36f", "misa-defeat-target");
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                MinionView(minion.MinionId).transform.position), Is.True);
            yield return null;
            yield return null;
            // Milestone 8.46e, per 8.46b: an exactly-one target resolves on its own press, so the window
            // draws no DONE for it; the window's model is asked, and null means nothing to press.
            var confirm = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
            if (confirm != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
                yield return null;
                yield return null;
            }
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion.MinionId)), Is.False,
                "The accepted printed Defeat must remove this unprotected adjacent minion.");
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(Object.FindObjectsByType<GuardsDigital.Presentation.Units.Minions.StandardMinionView>()
                .Any(view => view.BoundMinionId.Equals(minion.MinionId)), Is.False,
                "The removed minion's visible token must leave after its presentation settles.");
            yield return UiPlayer.CaptureGameView("8-36f", "misa-defeat-complete");
        }

        /// <summary>
        /// Watch How I Soar: "Choose one — • Place yourself into a space at maximum range. • Defeat a
        /// minion adjacent to you."
        ///
        /// <para>Rulebook p.10's modal form on the roster's first basic card that is not an Attack,
        /// and the first face of hers a player meets. Both bullets have a subject on this board, which
        /// is what makes the choice a real one: twenty-two empty spaces stand at exactly Range 4, and
        /// an enemy minion stands beside her. The adjacent friendly minion is not a legal Defeat.</para>
        ///
        /// <para><b>The journey takes the first bullet, and RULING 1 is what it measures.</b> "At
        /// maximum range" is EXACTLY the printed number, so the offered destinations are a RING of
        /// radius four rather than a disk: every offered space is at distance four and none is nearer.
        /// That is a claim a disk-shaped Place would fail, and it is asserted over the whole offered
        /// set rather than over the one space clicked.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator WatchHowISoar_OffersItsTwoBulletsAndPlacesHerOnTheRingTheFirstOneNames()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return CommitEverySeat(
                root, misaId, NameOf(runtime, MisaCardContent.WatchHowISoar));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Watch How I Soar. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "p.10's modal asks which bullet before either bullet asks anything. " +
                Remaining(runtime));
            // Milestone 8.46e: the bullets are the decision window's own controls now, and the Place
            // arm is pressed by the branch node it stands for rather than by its position.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2),
                "Both printed bullets are drawn as their own controls. Her Ultimate is locked on " +
                "this staging, so p.10's ceiling is one of them - which is the printed card. " +
                Remaining(runtime));

            UiPlayer.ClickEffectOption(runtime, hud, "soar-place-arm");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The chosen bullet opens its own destination question. " + Remaining(runtime));

            // RULING 1, measured over the whole offer rather than over one space: "at maximum range"
            // is a RING. Every candidate stands at exactly the printed Range of four, and a disk would
            // put nearer spaces in this same set.
            var offered = LitDestinations(runtime);
            Assert.That(offered, Is.Not.Empty,
                "Twenty-two empty spaces stand at exactly Range 4 from her staged space, measured " +
                "offline before this journey was written. " + Remaining(runtime));
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(space, stoodAt) == 4), Is.True,
                "RULING 1: every offered destination stands at EXACTLY the printed Range. A space " +
                "nearer than four in this set would be the disk reading the ruling overruled. " +
                "Offered: " + string.Join(", ", offered.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));

            var landing = offered[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(landing));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True,
                "The destination is chosen by clicking the space on the board.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(landing),
                "The Place puts her on the space she clicked. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(landing),
                "and her token is standing there rather than where she was.");
        }

        /// <summary>
        /// Swoop In: "Place yourself into a space in radius adjacent to two or more enemy units. If
        /// you do, you may retrieve a discarded card."
        ///
        /// <para>Her Silver basic, and the first destination test in this build that qualifies the
        /// SPACE being considered rather than the space the actor stands in. Four such spaces exist on
        /// the staged board, measured offline before this journey was written.</para>
        ///
        /// <para><b>The retrieve is the honest half.</b> "If you do, you MAY retrieve a discarded
        /// card" is an optional step behind a condition, so this journey drives it to the point where
        /// the offer either stands or does not - and asserts the printed shape rather than a
        /// particular outcome, because at this point in the round her discard may legitimately be
        /// empty and p.10's optional step then simply does not happen.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SwoopIn_PlacesHerBesideTwoEnemiesAndOffersTheRetrieveOnItsOwnConfirm()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Swoop In prints Initiative 6, so the enemy has to commit something lower for her to act
            // first. CommitSoTheNamedSeatActs picks that card rather than guessing one.
            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SwoopIn);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Swoop In. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Place opens its destination question. " + Remaining(runtime));
            var offered = LitDestinations(runtime);
            Assert.That(offered, Is.Not.Empty,
                "Four spaces in her printed Radius 3 stand beside two or more enemy units at this " +
                "staging, measured offline before this journey was written. " + Remaining(runtime));
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 3), Is.True,
                "\"In radius\" is p.9's disk, so every candidate is within the printed three - which " +
                "is the difference from Watch How I Soar's ring on the same hero.");

            var landing = offered[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(landing));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(landing),
                "The Place puts her on the qualifying space she clicked. " + Remaining(runtime));

            // The retrieve. "If you do, you may retrieve a discarded card" - an optional step behind a
            // condition the Place just satisfied.
            //
            // MILESTONE 8.86: this arm used to accept either outcome without saying which the board
            // was in, so an offer that never opened would have passed it silently. The two are now
            // TIED TO HER OWN PILE, which is the only thing the printed sentence reads: a card in it
            // means the step must be offered, an empty one means p.10's optional step does not
            // happen and THAT is what is asserted.
            var pileWhenAsked = runtime.Cards.Count(card =>
                card.OwnerId.Equals(misaId) && card.State.Location == CardLocation.DiscardPile);
            var standing = runtime.PendingEffectChoice;
            Assert.That(standing != null, Is.EqualTo(pileWhenAsked > 0),
                "\"If you do, you may retrieve a discarded card\" - the step is offered exactly " +
                "while her own pile holds one. Her pile holds " + pileWhenAsked + ". " +
                Remaining(runtime));
            if (standing != null)
            {
                // Milestone 8.46e: the retrieve's decline and its card chooser are the decision
                // window's own controls, read off its model; a card-valued choice is pressed on the
                // face the window draws for it, the way every card chooser is pressed.
                var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
                Assert.That(decline, Is.Not.Null,
                    "A `may` step is answerable both ways, so the standing prompt must draw its own " +
                    "decline. " + Remaining(runtime));

                var cards = runtime.DecisionWindowView.Model.Controls
                    .Where(control => control.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        control.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                    .ToArray();
                // MILESTONE 8.86: the offer is her own pile, face for face, and the step is TAKEN
                // rather than declined - a journey may not prove a retrieve by refusing it.
                Assert.That(cards, Is.Not.Empty,
                    "Her pile holds " + pileWhenAsked + " cards, so the retrieve draws a face for " +
                    "each one to choose from. " + Remaining(runtime));
                Assert.That(
                    cards.All(control => runtime.Cards
                        .Single(card => card.Id.Equals(
                            control.Command.EffectChoice.Candidate.Value.CardInstanceId))
                        .OwnerId.Equals(misaId)),
                    Is.True,
                    "and nobody retrieves another hero's card. " + Remaining(runtime));
                var retrieved = cards[0].Command.EffectChoice.Candidate.Value.CardInstanceId;
                if (cards[0].Card != null)
                {
                    UiPlayer.Click(UiPlayer.NamedElementWithText(
                        hud, "gd-effect-card", cards[0].Card.DisplayName.ToUpperInvariant()));
                }
                else
                {
                    UiPlayer.Click(UiPlayer.Button(hud, cards[0].Label));
                }

                yield return null;
                yield return null;
                Assert.That(
                    runtime.Cards.Single(card => card.Id.Equals(retrieved)).State.Location,
                    Is.EqualTo(CardLocation.Hand),
                    "THE PRINTED OUTCOME: the face she took really left the pile for her hand. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.Cards.Count(card =>
                        card.OwnerId.Equals(misaId) &&
                        card.State.Location == CardLocation.DiscardPile),
                    Is.EqualTo(pileWhenAsked - 1),
                    "and the pile is exactly one card shorter. " + Remaining(runtime));
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card finishes once the optional retrieve is answered or declined. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Challenge Accepted: "Target a unit adjacent to you. After the attack: This turn: Gain +2
        /// [Defense] Defense."
        ///
        /// <para>Her Red Tier I and the whole of her Durable role in one sentence. It targets the
        /// enemy Ranged minion standing beside her - NOT the enemy hero, who is two spaces away at
        /// this staging - and its rider raises her own Defense for the rest of the turn.</para>
        ///
        /// <para><b>The Active effect is read where it stands rather than inferred from a number.</b>
        /// The rider is p.14's step 7 text, so the pause it opens is an after-attack one, and what it
        /// creates is an effect the hero's own row carries until the turn ends.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChallengeAccepted_TargetsTheMinionBesideHerAndRaisesHerDefenseForTheTurn()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.ChallengeAccepted);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Challenge Accepted. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "p.14's step 2 asks whom, and her melee Red prints its own \"adjacent to you\". " +
                Remaining(runtime));

            // THE ENEMY HERO IS TWO SPACES AWAY at this staging, so "a unit adjacent to you" cannot
            // reach him. This is milestone 8.35z15's recorded trap taken seriously: a journey that
            // assumed a hero target here would have driven nothing, and the adjacent enemy RANGED
            // MINION is the honest subject.
            var candidates = TargetsWithSpaces(runtime);
            Assert.That(candidates, Is.Not.Empty,
                "An enemy Ranged minion stands beside her at this staging. " + Remaining(runtime));
            Assert.That(
                candidates.All(candidate =>
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt) == 1),
                Is.True,
                "\"Adjacent to you\" is radius one and nothing else, so every candidate stands " +
                "beside her - and the enemy hero, at two, is correctly not among them. Candidates: " +
                string.Join(", ", candidates.Select(candidate =>
                    $"({candidate.Space.Q},{candidate.Space.R})@" +
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt))));

            // MILESTONE 8.86: named by its own id rather than taken first off the offer.
            var minion = candidates
                .Where(candidate => candidate.Target.Kind == AttackTargetKind.Minion)
                .OrderBy(candidate => candidate.Target.MinionId.Value, System.StringComparer.Ordinal)
                .First();
            var struckId = minion.Target.MinionId;
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, MinionView(struckId).transform.position),
                Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;
            // THE ATTACK'S OWN OUTCOME (milestone 8.86). The journey asserted the candidate SET and
            // the rider's Active effect and never the Attack itself, so a card that asked whom and
            // then did nothing to them would have passed it.
            Assert.That(
                runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(struckId)), Is.False,
                "p.14 defeats an attacked minion without a Defense step, so the piece she named " +
                "leaves the board. " + Remaining(runtime));

            // p.14's step 7 rider. Whatever confirms or declines stand between here and the end of the
            // action are pressed the way a player presses them.
            for (var budget = 0; budget < 6 && runtime.PendingEffectChoice != null; budget++)
            {
                // Milestone 8.46e: whatever stands is read off the decision window's model - the
                // strip no longer draws a family the window has taken.
                var control = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm)
                    ?? UiPlayer.DecisionControl(runtime, command =>
                        command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                    ?? UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
                if (control == null)
                {
                    break;
                }

                UiPlayer.Click(UiPlayer.Button(hud, control.Label));
                yield return null;
                yield return null;
            }

            // The Active effect, read against the card that raised it rather than inferred from a
            // number: a guard nobody can attribute is a Defense that appears from nowhere.
            var challengeCardId = runtime.Cards
                .Single(card => card.OwnerId.Equals(misaId) &&
                    card.DefinitionReference.DefinitionId.Equals(MisaCardContent.ChallengeAccepted))
                .Id;
            Assert.That(runtime.IsActiveEffectSource(challengeCardId), Is.True,
                "\"This turn: Gain +2 Defense\" creates an effect the hero carries, and a rider that " +
                "created nothing would be a card that did nothing. " + Remaining(runtime));
        }

        /// <summary>
        /// Sudden Breeze: "Move up to 3 spaces in a straight line, ignoring obstacles; you may place an
        /// enemy unit you moved through into a space adjacent to you."
        ///
        /// <para>Her Blue Tier I, and the face RULING 29 settles. Three straight lines of three or
        /// less end on a playable space AND cross a unit at this staging - measured offline before
        /// this journey was written - so the rider has a subject rather than being driven past.</para>
        ///
        /// <para><b>Both halves of the sentence are separate questions and this journey answers
        /// both.</b> Where the line ends is a space clicked on the board; whom it crossed is a piece
        /// clicked on the board; and whether to place them is p.10's own `may`.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SuddenBreeze_CrossesTheEnemyOnItsLineAndOffersToPlaceThemBesideHer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SuddenBreeze);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Sudden Breeze. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The line asks where it ends before anything else happens. " + Remaining(runtime));
            var ends = LitDestinations(runtime);
            Assert.That(ends, Is.Not.Empty, Remaining(runtime));
            Assert.That(
                ends.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 3), Is.True,
                "\"Up to 3 spaces\" bounds the line at three, and a straight line's distance is its " +
                "own length.");

            // THE MEASURED CROSSING LINE, asserted rather than preferred. (-5, 3) is two spaces from
            // her staged space in one straight direction and its interior holds the enemy Ranged
            // minion at (-4, 2); milestone 8.35z38 measured that offline, and separately read
            // `ObstacleQuery.BlocksTransit` to confirm that an OCCUPIED playable space does not block
            // transit once `ignoresObstacles` is bound - which is what her five Blues bind and what
            // RULING 29 settles. If this end is not offered, the line chooser is refusing to cross a
            // unit that the card's own words cross, and the whole Blue rider family is unreachable.
            // That is a finding rather than a staging accident, which is why it fails here instead of
            // quietly taking a different end and driving nothing.
            var crossing = new HexCoordinate(-5, 3);
            Assert.That(ends, Does.Contain(crossing),
                "RULING 29: a straight line whose interior holds a unit must be offered, because " +
                "\"ignoring obstacles\" is what the card prints. Offered ends: " +
                string.Join(", ", ends.Select(space => $"({space.Q},{space.R})")));

            var chosen = crossing;

            // A card-text straight-line Move is p.11's own movement session - the hero TRAVELS it -
            // so the named end may take more than one click to reach. See Dash and Slash's journey,
            // which is where milestone 8.35z38 measured that: at four spaces the prompt stood again
            // after the first click with her piece not yet moved. Bounded by the printed three.
            for (var step = 0;
                step < 5 &&
                !runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate.Equals(chosen);
                step++)
            {
                var remaining = LitDestinations(runtime);
                if (remaining.Length == 0)
                {
                    break;
                }

                var tile = Object.FindObjectsByType<HexTileView>()
                    .Single(value => value.Coordinate.Equals(
                        remaining.Contains(chosen) ? chosen : remaining[0]));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
                yield return null;
                yield return null;
            }

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(chosen),
                "The Move walks her to the end of the line she named. " + Remaining(runtime));

            // Whom she crossed - a piece clicked on the board - and then p.10's own `may`.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The rider asks whom she moved through. " + Remaining(runtime));
            var crossed = MinionsAt(AnyOfferedSpaces(runtime)).ToArray();
            Assert.That(crossed, Is.Not.Empty,
                "The chosen line's interior holds the enemy Ranged minion, measured offline. " +
                Remaining(runtime));
            // MILESTONE 8.86: WHICH piece she crossed is named and its space read, because "you may
            // place an enemy unit you moved through into a space adjacent to you" is a promise about
            // where that piece ends up - and this journey used to press its way to the end of the
            // card without ever looking at it again.
            var crossedId = crossed[0].BoundMinionId;
            var crossedFrom = runtime.ReadModel.Minions
                .Single(minion => minion.MinionId.Equals(crossedId)).Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, crossed[0].transform.position), Is.True);
            yield return null;
            yield return null;

            var placedAt = default(HexCoordinate?);
            for (var budget = 0; budget < 4 && runtime.PendingEffectChoice != null; budget++)
            {
                var targets = AnyOfferedSpaces(runtime);
                if (targets.Length > 0)
                {
                    // A NON-ZERO answer, deliberately: the place is TAKEN rather than refused,
                    // because a journey may not prove a placement by declining it.
                    var space = Object.FindObjectsByType<HexTileView>()
                        .Single(value => value.Coordinate.Equals(targets[0]));
                    Assert.That(
                        UiPlayer.ClickBoard(pointerInput, hud, space.transform.position), Is.True);
                    placedAt = targets[0];
                }
                else
                {
                    // Milestone 8.46e: the printed may is declined on the decision window, read off its model.
                    var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
                    if (decline == null)
                    {
                        break;
                    }

                    UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                }

                yield return null;
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card finishes once the rider is answered. " + Remaining(runtime));
            // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86).
            Assert.That(placedAt, Is.Not.Null,
                "\"you may place an enemy unit you moved through into a space adjacent to you\" - " +
                "the rider has to have offered a space to put the crossed piece on. " +
                Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions
                    .Single(minion => minion.MinionId.Equals(crossedId)).Coordinate,
                Is.EqualTo(placedAt.Value),
                "and the piece she crossed is standing on the space she placed it on, rather than " +
                "where the line passed over it. " + Remaining(runtime));
            Assert.That(
                HexDistanceQuery.Calculate(placedAt.Value,
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate),
                Is.EqualTo(1),
                "\"adjacent to you\", measured from where the line left her. " + Remaining(runtime));
            Assert.That(placedAt.Value, Is.Not.EqualTo(crossedFrom),
                "and the placement really moved it. " + Remaining(runtime));
        }

        /// <summary>
        /// Focus: "Next turn: Before you perform a primary action, you may move 1 space."
        ///
        /// <para>Her Green Tier I, and the face RULING 31 settles: the free move fires ONCE. The user's
        /// words are "It says action, not actions. So just the once."</para>
        ///
        /// <para><b>What a journey can honestly prove about it, and what it cannot.</b> The effect is
        /// raised on one turn and offered on the NEXT, which is a round boundary away. This journey
        /// plays the card, reads the effect standing on her own row - milestone 8.35z29's
        /// <c>NextTurn</c> duration, carried across the turn boundary rather than expiring with the
        /// turn that made it - and then drives the round over and asserts the pre-action offer stands
        /// where p.15's own timing puts it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Focus_ArmsItsFreeStepAndCarriesItAcrossTheTurnBoundary()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var misaId = MisaSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.Focus);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Focus. " + Remaining(runtime));

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Focus asks a player nothing on the turn it is played: its whole sentence is a grant " +
                "for the turn after. " + Remaining(runtime));

            var focusCardId = runtime.Cards
                .Single(card => card.OwnerId.Equals(misaId) &&
                    card.DefinitionReference.DefinitionId.Equals(MisaCardContent.Focus))
                .Id;
            // THE TURN BOUNDARY, and the assertion is placed AFTER it rather than before - which
            // milestone 8.35z38's first Play Mode run is what settled. A `Next turn:` grant is not a
            // live source on the turn that RAISED it: `IsActiveEffectSource` reports what is applying
            // now, and this grant applies from the next turn. Asserting it immediately after the card
            // resolved read false, correctly, and the reading a journey should hold the card to is the
            // one the player meets - the step standing on a turn nobody raised it on.
            //
            // The round boundary is deliberately NOT crossed. p.7's Retrieve Cards step "cancels all
            // your Active effects, including 'Next Turn' effects", so a Focus played on the round's
            // last turn never takes effect at all - which the shard records as content rather than a
            // defect, and which is why this journey advances ONE turn instead of draining the round.
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);

            // THE NEXT TURN HAS TO BE OPENED BEFORE IT CAN BE READ, which milestone 8.35z38's second
            // Play Mode run is what settled: the assertion below ran while the status read "YOUR
            // COMMITMENT - CHOOSE A CARD, OR PASS", which is p.7's commitment phase and not a turn.
            // An effect that applies on a turn is not a live source while no turn is open, so a
            // journey that asserted there was asking the projection a question it cannot answer yet.
            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SwoopIn);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);

            Assert.That(runtime.IsActiveEffectSource(focusCardId), Is.True,
                "The free step is a NEXT-turn grant, so it stands on the turn AFTER the one that " +
                "raised it. Its absence here is the cancellation-window defect milestone 8.35z29's " +
                "finding F-6 records - a NextTurn effect must expire at " +
                "TurnOnlyCancellationBeforeTurnEnded rather than at TurnEnded. " + Remaining(runtime));
        }

        /// <summary>
        /// p.14's secondary Defense icon, spent as a whole face out of her own hand, with the Attack
        /// surviving rather than defeating her.
        ///
        /// <para>Misa is the roster's Durable hero and this is the plain half of that: the icon on the
        /// card's own ribbon, answered without any card text at all.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndMisaSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, misaId);

            // Setup, headlessly: she walks into contact and the enemy attacks her on the next turn.
            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.WatchHowISoar);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            yield return WalkHeadlesslyTo(runtime, misaId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, misaId)), Is.True,
                "The enemy Attack must be able to name Misa, or there is no Defense to spend. " +
                Remaining(runtime));
            yield return null;

            // Her own answer, on the live surface: a whole card spent for the number on its ribbon.
            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null,
                "p.14 offers her every face in hand whose Defense answers this total, and at least " +
                "one of hers must survive it - she is the roster's Durable hero. " +
                Remaining(runtime));

            // A defense face is drawn as a CARD ELEMENT rather than a labelled button, which is the
            // idiom every hero's defense journey uses and which milestone 8.35z38's first Play Mode
            // run rediscovered: clicking by the control's own Label found nothing, because the shown
            // controls are three `gd-defense-card` elements beside DECLINE DEFENSE.
            var survivingFace = NameOfInstance(
                runtime, defense.DefenseBreakdown.CardInstanceId.Value);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            yield return null;

            Assert.That(
                runtime.Heroes.Any(hero => hero.HeroId.Equals(misaId)), Is.True,
                "The defended Attack leaves her standing. " + Remaining(runtime));
        }

        /// <summary>
        /// p.13's Defeat and respawn, on her own piece: an Attack she cannot answer takes her off the
        /// board, and a board click puts her back on her own spawn point.
        /// </summary>
        [UnityTest]
        public IEnumerator Misa_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, misaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.WatchHowISoar);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            yield return WalkHeadlesslyTo(runtime, misaId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, misaId)), Is.True, Remaining(runtime));
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndMisaSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(misaId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(misaId)), Is.False,
                "and her token is gone with her rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // p.13 offers the respawn at the defeated hero's own next turn, so the card she commits
            // for it has to take that turn - and it has to be one she still HOLDS. Watch How I Soar
            // opened this journey and is spent, which milestone 8.35z38's first Play Mode run found
            // as a bare "Sequence contains no matching element": a fixture that names the same card
            // twice in one journey is naming a card that is no longer in the hand. Sudden Breeze's
            // printed 10 is the highest of what remains.
            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SuddenBreeze);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Misa must be offered her respawn at her own turn. Status: " +
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
                .Where(view => view.Occupancy.HeroId.Equals(misaId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Misa token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is her own official token that came back.");
        }
    }
}

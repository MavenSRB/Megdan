using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Misa the Samurai's piece, her tray and the Director drawer, driven through real pointer events
    /// and camera rays. Milestone 8.35z38, and the eighth hero to take the player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Misa is not among them - the
    /// roster-expansion amendment has not placed her in an official line-up. Every arm that deals her
    /// restages both heroes into the engagement milestone 8.15 built.</para>
    ///
    /// <para><b>She needs no ally arm, and hers is the strongest case of that on the roster.</b> Not
    /// one of her nineteen faces names a friendly hero at all - no heal, no grant, no friendly
    /// selection. What her set names is enemy units, her own space, and the geometry between
    /// them.</para>
    ///
    /// <para><b>THE MEASURED GEOMETRY THAT SHAPES EVERY JOURNEY OF HERS.</b> Milestone 8.35z38
    /// measured the staged board offline before writing a line of these fixtures, at both positions,
    /// and the two answers are different enough to decide where each journey lives:</para>
    ///
    /// <list type="bullet">
    /// <item><description>AT SETUP the two heroes are FIFTEEN apart, no space in her Radius 3 stands
    /// beside two enemies, no straight line out of her space crosses anybody, and no two units share a
    /// distance from her within 3. Four of her printed clauses have no subject at all there.</description></item>
    /// <item><description>ENGAGED she stands at (-3, 1) and the enemy hero at (-2, 2), two apart. An
    /// enemy Ranged minion is ADJACENT to her at (-4, 2) and a friendly Melee minion at (-3, 2); there
    /// are 22 empty spaces at exactly Range 4, four Swoop In destinations, three straight lines of
    /// three or less that cross a unit, and three units standing at exactly Radius 3.</description></item>
    /// </list>
    ///
    /// <para><b>The two spaces between the heroes are the fact most of these journeys turn on</b>, and
    /// milestone 8.35z15 recorded why it must be stated rather than assumed: three shipped journeys of
    /// another hero had only ever DECLINED the prompt their names claimed to drive, because this
    /// staging holds the heroes two apart by construction. So her melee Reds cannot reach the enemy
    /// HERO without a step, and take the adjacent enemy minion instead; and her ranged pair, printing
    /// Range 2, finds that hero at EXACTLY maximum range - which is what makes RULING 32's past tense
    /// true on the untouched board rather than something a journey has to manufacture.</para>
    ///
    /// <para><b>The driving helpers are shared rather than copied.</b> This fixture imports the
    /// hero-agnostic steps the Xargatha fixtures already define, for the reason milestone 8.35z34
    /// recorded when it did the same: an eighth transcription of them would be an eighth place for the
    /// same step to drift. What is Misa's own is <see cref="MisaSeat"/> and the journeys.</para>
    /// </summary>
    public sealed class MisaUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnMisasOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Misa on Blue - so two pieces stand " +
                "and a third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var misaSetup = MisaSeat(runtime);
            var misaView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(misaSetup.HeroId));

            Assert.That(misaView.HeroContentId.HasValue, Is.True,
                "The Blue piece carries no content identity, so nothing on the board says which hero " +
                "it is.");
            Assert.That(misaView.HeroContentId.Value.Value,
                Is.EqualTo(MisaCardContent.ContentSetId),
                "The content identity is `misa` and nothing else - the epithet is a display label on " +
                "the seat, and no piece, rule or card is addressed by it.");
            Assert.That(misaView.TeamSide, Is.EqualTo(TeamSide.Blue),
                "She takes the Blue seat in this staging, exactly as the five staged heroes before " +
                "her do: milestone 8.25 owns the official six-player configuration.");
            Assert.That(misaView.HasTokenVisual, Is.True,
                "Misa's official circular token is prepared, so the piece must be wearing it rather " +
                "than the generated prism the fallback draws.");
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(misaSetup.HeroId)).DisplayName,
                Is.EqualTo("Misa the Samurai"),
                "The seat carries the rulebook's own setup-step-7 name.");

            Assert.That(misaView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(misaView.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-01"),
                "Misa takes the map's own first Blue spawn point rather than a space invented for " +
                "her; the staging moves the piece and leaves the spawn where p.6 declared it.");
            Assert.That(misaView.Coordinate, Is.EqualTo(misaSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(misaSetup.HeroId)).Coordinate,
                Is.EqualTo(misaView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(misaSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(MisaCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "Eighteen faces, because p.20's Leaf on the Wind is in the box.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(misaSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(misaSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(misaSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings it " +
                "back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, misaView.transform.position), Is.True,
                "The camera ray at her piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(misaSetup.HeroId),
                "Clicking her piece points the tray at her.");

            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltMisa = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(misaSetup.HeroId));
            Assert.That(rebuiltMisa.Coordinate, Is.EqualTo(misaSetup.Coordinate));
            Assert.That(rebuiltMisa.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(misaSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Misa_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;

            // She commits the highest-Initiative card in her hand that prints a secondary Movement
            // icon - Watch How I Soar, at 12 - and the enemy commits their lowest, so she takes the
            // first turn whichever seat the tray opened on. Rulebook p.7 resolves the higher printed
            // Initiative first.
            //
            // SWOOP IN is the one card of hers this step can never pick, and that is content rather
            // than an accident: her Silver basic prints a Defense 3 shield and no boot at all, which
            // is why its matrix row carries no secondary-movement shape where every other dealt face
            // of hers does. It is the same shape Dodger's Death Trap has, on the other basic.
            yield return CommitEverySeat(root, misaId, MovingCardOf(runtime, misaId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "Misa's higher Initiative gives her p.7's first turn. " + Remaining(runtime));
            Assert.That(runtime.GetCommitmentStatus(misaId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            var acceptedBeforeStart = runtime.AcceptedCommands.Count;
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBeforeStart + 1),
                "One click on the Movement control is one accepted command.");
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The clicked control must open p.10's movement session.");

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            var misaView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(misaId));
            Assert.That(misaView.Coordinate, Is.EqualTo(destination),
                "The token must be standing where the hero now is, not where she was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Null);
            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(misaId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Misa's spent card reads as this round's first turn on her own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesMisasCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var misaSetup = MisaSeat(runtime);
            yield return PointTrayAt(root, misaSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, misaSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(misaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(misaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Misa's committed card.");

            yield return PointTrayAt(root, misaSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, misaSetup.HeroId));
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(misaSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2),
                "A restart that leaves a duplicate piece behind is what this counts.");
            var misaView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(misaSetup.HeroId));
            Assert.That(misaView.Coordinate, Is.EqualTo(misaSetup.Coordinate),
                "Misa stands on her configured space again.");
            Assert.That(misaView.HasTokenVisual, Is.True,
                "and is still wearing her own official token.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(misaSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(MisaCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        /// <summary>
        /// Misa's half of milestone 8.20c's standard-footprint conformance: every one of her verified
        /// faces, drawn one at a time on the live HUD panel and measured against <c>CardFootprint</c>.
        ///
        /// <para>Her set is a real test of it rather than a formality, for a reason no earlier hero's
        /// supplies: Watch How I Soar prints rulebook p.10's modal form with two bullets, and Power
        /// Overwhelming prints TWO INDEPENDENT PARAGRAPHS in one Ultimate text box - the widest text a
        /// verified face in this build carries.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryMisaFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMisa);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Misa", MisaCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The staged seat dealt Misa's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState MisaSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == MisaCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Misa to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// The first one-cost movement destination whose own camera ray reaches the board rather than
        /// a hero piece standing over it.
        /// </summary>
        private static HexCoordinate FirstClickableStep(
            PrimitiveHeroPointerInput pointerInput, ConfiguredMatchSceneRuntime runtime)
        {
            var steps = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(steps, Is.Not.Empty, "The opened movement session offers no one-space step.");
            return FirstClickableSpace(pointerInput, steps);
        }

        /// <summary>
        /// Every legal target of a standing rulebook p.14 step-2 question, paired with the space its
        /// piece stands on - which is what a journey needs to click one and to say how far away it is.
        ///
        /// <para>It is here rather than in a hero-agnostic file for the reason milestone 8.35z34 gave
        /// for leaving the shared steps where they are: extracting it would touch seven shipped hero
        /// fixtures, which is a refactor and not a dealing. Misa is the first hero whose journeys need
        /// the DISTANCE of a p.14 target rather than only its identity, because RULING 1 and RULING 32
        /// are both about how far away a unit is.</para>
        /// </summary>
        internal static (Core.Rules.CardResolution.AttackTarget Target, HexCoordinate Space)[]
            TargetsWithSpaces(ConfiguredMatchSceneRuntime runtime)
        {
            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, "No p.14 step-2 question is standing.");
            return pending.LegalTargetHeroIds
                .Select(heroId => (
                    Core.Rules.CardResolution.AttackTarget.Hero(heroId),
                    HeroView(heroId).Coordinate))
                .Concat(pending.LegalTargetMinionIds
                    .Select(minionId => (
                        Core.Rules.CardResolution.AttackTarget.Minion(minionId),
                        MinionView(minionId).Occupancy.Coordinate)))
                .ToArray();
        }

        /// <summary>
        /// The drawn piece one legal target names, whichever kind it is - so a journey clicks the same
        /// way for a hero and for a minion, which is what a player does.
        /// </summary>
        /// <summary>
        /// Every space a standing effect pause will accept a board click on, whichever of the two
        /// projection lists it arrived in.
        ///
        /// <para><b>The distinction is real and this milestone learned it the expensive way.</b> A
        /// selection among UNITS publishes its candidates as effect-choice TARGETS; a selection among
        /// SPACES publishes them as effect-choice DESTINATIONS, and the two lists are disjoint. Misa's
        /// first Play Mode run failed six journeys on exactly that - the status line read "Choose 1 of
        /// 22 spaces" and listed all twenty-two while the targets list was empty, which is the offer
        /// working and the fixture asking the wrong list. The specific assertions in her journeys name
        /// the list they mean; this helper exists for the DRIVE LOOPS, which answer whatever a card
        /// happens to open next and have no business caring.</para>
        /// </summary>
        internal static HexCoordinate[] AnyOfferedSpaces(ConfiguredMatchSceneRuntime runtime)
        {
            var destinations = LitDestinations(runtime);
            return destinations.Length > 0
                ? destinations
                : runtime.RoundView.Model.EffectChoiceTargets
                    .Select(candidate => candidate.Space)
                    .ToArray();
        }

        /// <summary>
        /// Answers whatever effect decision is standing, through the projected command the HUD's own
        /// control carries - a destination, a selection, a confirm or a decline, in that order.
        /// Returns false when nothing is standing that it knows how to answer.
        ///
        /// <para><b>This is SETUP and is deliberately not a click.</b> It exists for the tail of a
        /// journey whose SUBJECT is something else - the offer a ruling shapes, or the clause a
        /// ruling arms - and which has to get a multi-step card finished afterwards. Milestone
        /// 8.35z38's third Play Mode run is why it exists rather than a click loop: a camera ray at a
        /// distant tile can reach the board and still land on a NEARER tile that is not a candidate,
        /// so a driver clicking its way down a long offer answers nothing and spends its whole budget
        /// doing it, twice over. The board click for a space is proved where it is the subject - by
        /// Watch How I Soar's ring and Sudden Breeze's line - and repeating it in a drive loop proves
        /// nothing those two do not.</para>
        /// </summary>
        /// <summary>
        /// Whether <see cref="AnswerStandingChoiceHeadlessly"/> has anything to answer right now.
        ///
        /// <para>Milestone 8.35z53 added this because <c>runtime.PendingEffectChoice</c> is NOT the
        /// same question. A drain loop conditioned on that property sees a standing MODE or CARD
        /// choice and is blind to a standing DESTINATION one - the surface a forced move presents -
        /// so the loop body never runs and the next `SubmitEndTurn` fails against a decision nobody
        /// answered. Whisper's dealing is where that first bit: her Gold basic's rider asks the hero
        /// who DEFENDED for a space, so a setup arc that drives an Attack-and-Defence exchange to
        /// fill a discard pile leaves a destination standing that no earlier hero's arc could.</para>
        ///
        /// <para>It mirrors <see cref="AnswerStandingChoiceHeadlessly"/>'s own selection exactly
        /// rather than re-deriving it, so the two cannot come to disagree about what is answerable.</para>
        /// </summary>
        internal static bool AStandingChoiceCanBeAnswered(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            return model.EffectChoiceDestinations.Any() ||
                model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select ||
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm ||
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
        }

        internal static bool AnswerStandingChoiceHeadlessly(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            var control = model.EffectChoiceDestinations.FirstOrDefault() ??
                model.EffectChoices.FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select) ??
                model.EffectChoices.FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm) ??
                model.EffectChoices.FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            if (control == null)
            {
                return false;
            }

            return runtime.SubmitProjectedCommand(control.Command);
        }

        /// <summary>
        /// The identity of whatever unit is standing on this space, or an empty string when the space
        /// is free. MILESTONE 8.86: a Swap is asserted by reading the two spaces before and after,
        /// which is the only reading that can tell an exchange from a prompt that merely closed.
        /// </summary>
        internal static string UnitOn(ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Heroes
                .Where(hero => hero.Coordinate.Equals(space))
                .Select(hero => "hero:" + hero.HeroId.Value)
                .Concat(runtime.ReadModel.Minions
                    .Where(minion => minion.Coordinate.Equals(space))
                    .Select(minion => "minion:" + minion.MinionId.Value))
                .FirstOrDefault() ?? string.Empty;

        /// <summary>
        /// Where the piece one rulebook p.14 target names is standing NOW, or null when it has left
        /// the board. MILESTONE 8.86: a rider that shoves, places or swaps "the target" is read on
        /// this before and after the press, because nothing else can tell a displacement from a step
        /// that never ran.
        /// </summary>
        internal static HexCoordinate? SpaceOfTarget(
            ConfiguredMatchSceneRuntime runtime, Core.Rules.CardResolution.AttackTarget target) =>
            target.Kind == Core.Rules.CardResolution.AttackTargetKind.Hero
                ? runtime.Heroes
                    .Where(hero => hero.HeroId.Equals(target.HeroId))
                    .Select(hero => (HexCoordinate?)hero.Coordinate)
                    .FirstOrDefault()
                : runtime.ReadModel.Minions
                    .Where(minion => minion.MinionId.Equals(target.MinionId))
                    .Select(minion => (HexCoordinate?)minion.Coordinate)
                    .FirstOrDefault();

        /// <summary>
        /// Every space beside <paramref name="from"/> that is free, playable and strictly farther
        /// from <paramref name="actor"/> - the whole set milestone 8.35z29's <c>AwayFromActor</c>
        /// policy can put a shoved piece on.
        ///
        /// <para>MILESTONE 8.86: a journey that meets an EMPTY shove offer may not conclude from that
        /// absence. It asserts THIS instead - that the board really leaves the printed sentence
        /// nowhere to put the target - which is the difference between a card that did nothing and a
        /// card that could do nothing.</para>
        /// </summary>
        internal static HexCoordinate[] FreeSpacesFartherFrom(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate from, HexCoordinate actor)
        {
            var occupied = runtime.Projection.Heroes.Select(hero => hero.Coordinate)
                .Concat(runtime.Projection.Minions.Select(minion => minion.Coordinate))
                .Concat(runtime.Projection.Obstacles.Select(obstacle => obstacle.Coordinate))
                .ToArray();
            return from.GetNeighbors()
                .Where(space =>
                    HexDistanceQuery.Calculate(space, actor) >
                        HexDistanceQuery.Calculate(from, actor) &&
                    runtime.Projection.SelectedMap.Spaces.Any(value =>
                        value.Coordinate.Equals(space) &&
                        value.Kind == BoardMapSpaceKind.Playable) &&
                    !occupied.Any(value => value.Equals(space)))
                .ToArray();
        }

        internal static Component ViewOf(Core.Rules.CardResolution.AttackTarget target) =>
            target.Kind == Core.Rules.CardResolution.AttackTargetKind.Hero
                ? (Component)HeroView(target.HeroId)
                : MinionView(target.MinionId);
    }
}

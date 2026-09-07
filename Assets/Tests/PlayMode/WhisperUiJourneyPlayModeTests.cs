using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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
    /// Whisper the Outcast's piece, her tray and the Director drawer, driven through real pointer
    /// events and camera rays. Milestone 8.35z53, and the ninth hero to take the
    /// player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Whisper is not among them - she
    /// is the first hero of the Devoted Hero pack and the roster-expansion amendment has not placed her
    /// in an official line-up. Every arm that deals her restages both heroes into the engagement
    /// milestone 8.15 built.</para>
    ///
    /// <para><b>She needs no ally arm.</b> Not one of her nineteen faces names a friendly hero: no
    /// heal, no grant, no friendly selection. What her set names is enemy units, empty minion Spawn
    /// points, her own space, and - five times over - the enemy's own DISCARD PILE.</para>
    ///
    /// <para><b>THE TWO FACTS THAT SHAPE EVERY JOURNEY OF HERS.</b> The first is geometry and is
    /// hero-independent: <c>EngagedMirrorStaging</c> restages whichever two heroes it is given onto the
    /// same pair of spaces, so the Blue seat stands at (-3, 1) and the Red seat at (-2, 2), TWO apart.
    /// Milestone 8.35z38 measured that board in full for Misa and this milestone reuses the
    /// measurement rather than re-deriving it, because the staging does not depend on who is
    /// standing on it. What follows for Whisper: her printed Range 3 Gold basic reaches the enemy hero
    /// comfortably, and her whole Red line - which ends in "Target a unit adjacent to you" - reaches
    /// the adjacent enemy Ranged minion at (-4, 2) without moving, and the enemy HERO only after a
    /// step.</para>
    ///
    /// <para><b>The second fact is not geometry at all and no staging can change it: AT THE FIRST TURN
    /// OF ROUND 1 NO DISCARD PILE IN THE MATCH HOLDS ANYTHING.</b> p.6 deals every card to a hand or a
    /// deck, and a pile fills only when somebody spends a card. Five of Whisper's faces read a pile -
    /// Death Seeker's whole gate, Blood Fury's and Blood Frenzy's repeat gate, and the printed-Range
    /// arm of both Swaps - so all five are unstartable or narrowed on turn one and come alive from turn
    /// two. RULING 42 accepts that as printed rather than as a defect, and
    /// <see cref="WhisperEngagedUiJourneyPlayModeTests
    /// .DeathSeeker_IsUnstartableOnTheFirstTurnOfRoundOne_NoPileHoldingAnythingYet"/> is the journey
    /// that proves the accepted behaviour instead of routing around it. THE INVERSE is what makes her
    /// Gold basic's first bullet reachable on turn one and only then: it wants a hero with an EMPTY
    /// discard, and on the opening turn every hero has one.</para>
    ///
    /// <para><b>The driving helpers are shared rather than copied.</b> This fixture imports the
    /// hero-agnostic steps the Xargatha fixtures already define, for the reason milestones 8.35z34 and
    /// 8.35z38 recorded when they did the same: a ninth transcription of them would be a ninth place
    /// for the same step to drift. What is Whisper's own is <see cref="WhisperSeat"/> and the
    /// journeys.</para>
    /// </summary>
    public sealed class WhisperUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnWhispersOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Whisper on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var whisperSetup = WhisperSeat(runtime);
            var whisperView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(whisperSetup.HeroId));

            Assert.That(whisperView.HeroContentId.HasValue, Is.True,
                "The Blue piece carries no content identity, so nothing on the board says which hero " +
                "it is.");
            Assert.That(whisperView.HeroContentId.Value.Value,
                Is.EqualTo(WhisperCardContent.ContentSetId),
                "The content identity is `whisper` and nothing else - the epithet is a display label " +
                "on the seat, and no piece, rule or card is addressed by it.");
            Assert.That(whisperView.TeamSide, Is.EqualTo(TeamSide.Blue),
                "She takes the Blue seat in this staging, exactly as the six staged heroes before " +
                "her do: milestone 8.25 owns the official six-player configuration.");
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(whisperSetup.HeroId)).DisplayName,
                Is.EqualTo("Whisper the Outcast"),
                "The seat carries the rulebook's own setup-step-7 name.");

            Assert.That(whisperView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(whisperView.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-01"),
                "Whisper takes the map's own first Blue spawn point rather than a space invented " +
                "for her; the staging moves the piece and leaves the spawn where p.6 declared it.");
            Assert.That(whisperView.Coordinate, Is.EqualTo(whisperSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperSetup.HeroId)).Coordinate,
                Is.EqualTo(whisperView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(whisperSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(WhisperCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "Eighteen faces, because p.20's Vile Accusation is in the box.");

            // THE OPENING DISCARD PILES ARE EMPTY, and this is where that is pinned rather than
            // assumed - it is the precondition five of her faces read and one of her bullets reads the
            // negative of, so a journey elsewhere that found it false would be measuring a board this
            // one never described.
            Assert.That(
                runtime.Cards.Any(card => card.State.Location == CardLocation.DiscardPile), Is.False,
                "At p.6's setup every card is in a hand or a deck, so no pile in the match holds " +
                "anything - which is what makes Death Seeker unstartable on turn one (RULING 42) and " +
                "Swift Justice's first bullet reachable on it.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(whisperSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(whisperSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(whisperSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings it " +
                "back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, whisperView.transform.position), Is.True,
                "The camera ray at her piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(whisperSetup.HeroId),
                "Clicking her piece points the tray at her.");

            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltWhisper = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(whisperSetup.HeroId));
            Assert.That(rebuiltWhisper.Coordinate, Is.EqualTo(whisperSetup.Coordinate));
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(whisperSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Whisper_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;

            // She commits the highest-Initiative card in her hand that prints a secondary Movement
            // icon - Swift Justice, at 12 - and the enemy commits their lowest, so she takes the first
            // turn whichever seat the tray opened on. Rulebook p.7 resolves the higher printed
            // Initiative first.
            //
            // DEATH SEEKER is the one card of hers this step can never pick, and that is content
            // rather than an accident: her Silver basic prints a Defense 2 shield and NO BOOT AT ALL,
            // which is why its matrix row carries no secondary-movement shape where every other dealt
            // face of hers does. It is the same shape Misa's Swoop In and Dodger's Death Trap have, on
            // the other basic.
            yield return CommitEverySeat(root, whisperId, MovingCardOf(runtime, whisperId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "Whisper's higher Initiative gives her p.7's first turn. " + Remaining(runtime));
            Assert.That(runtime.GetCommitmentStatus(whisperId),
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

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            var whisperView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(whisperId));
            Assert.That(whisperView.Coordinate, Is.EqualTo(destination),
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
                .Single(hand => hand.HeroId.Equals(whisperId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Whisper's spent card reads as this round's first turn on her own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesWhispersCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var whisperSetup = WhisperSeat(runtime);
            yield return PointTrayAt(root, whisperSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, whisperSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(whisperSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(whisperSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Whisper's committed card.");

            yield return PointTrayAt(root, whisperSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, whisperSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(whisperSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2),
                "A restart that leaves a duplicate piece behind is what this counts.");
            var whisperView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(whisperSetup.HeroId));
            Assert.That(whisperView.Coordinate, Is.EqualTo(whisperSetup.Coordinate),
                "Whisper stands on her configured space again.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(whisperSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(WhisperCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        /// <summary>
        /// Whisper's half of milestone 8.20c's standard-footprint conformance: every one of her
        /// verified faces, drawn one at a time on the live HUD panel and measured against
        /// <c>CardFootprint</c>.
        ///
        /// <para>Her set is a real test of it rather than a formality, for a reason no earlier hero's
        /// supplies: Swift Justice prints rulebook p.10's modal form with two bullets AND a p.14 timing
        /// keyword INSIDE the first of them - the only face in the build that carries a moment keyword
        /// inside a bullet - and Lesser Evil prints two bullets plus a parenthetical reminder clause,
        /// which is the longest Blue text a verified face carries.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryWhisperFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Whisper", WhisperCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The staged seat dealt Whisper's printed set, found by the content identity the
        /// configuration bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState WhisperSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == WhisperCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Whisper to exactly one seat; it dealt her to " +
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
        /// Every card instance sitting in any hero's discard pile right now, as definition ids. Five of
        /// Whisper's faces read this and one of her bullets reads its negative, so her journeys assert
        /// the pile's state directly rather than inferring it from what a card did.
        /// </summary>
        internal static string[] DiscardedDefinitionIds(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Cards
                .Where(card => card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id, System.StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// Whether the standing decision belongs to the hero named, read off the pending choice's own
        /// chooser - which is the field <c>SeatProjection.OwnerOfStandingDecision</c> reads and
        /// therefore the field milestone 8.35r's decision-owner routing sends a seat by.
        ///
        /// <para>It exists because Whisper is the first hero whose content puts a decision to a seat
        /// that is not the acting one through a CONTROL NODE rather than through a handler
        /// (<see cref="GuardsDigital.Core.Effects.EffectChooserRole.BoundHero"/>), so "whose question
        /// is this" is an assertion her journeys make and no earlier hero's needed to.</para>
        /// </summary>
        internal static bool StandingChoiceBelongsTo(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId)
        {
            var pending = runtime.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null,
                "No effect decision is standing, so there is no owner to read. " + Remaining(runtime));
            return pending.ChooserId.Equals(heroId);
        }
    }
}

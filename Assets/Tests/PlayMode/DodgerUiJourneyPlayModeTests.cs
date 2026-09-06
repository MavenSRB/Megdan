using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
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
    /// Dodger the Warlock's piece, her tray and the Director drawer, driven through real pointer
    /// events and camera rays. Milestone 8.35z34, and the seventh hero to take the player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Dodger is not among them - the
    /// roster-expansion amendment has not placed her in an official line-up. Every arm that deals her
    /// restages both heroes into the engagement milestone 8.15 built, so the piece rows below prove
    /// the spawn the two ways that staging leaves open: the token carries the map's own declared Blue
    /// spawn point, and p.13's respawn puts her back on it from a board click, which is
    /// <see cref="DodgerEngagedUiJourneyPlayModeTests.Dodger_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick"/>'s
    /// subject.</para>
    ///
    /// <para><b>She needs no ally arm.</b> Every clause of hers that names another hero names an
    /// ENEMY one - "an enemy hero in radius", "enemy heroes in radius", "a hero in range who has one
    /// or more cards in the discard" - so a friendly hero would widen no candidate set she has. The one
    /// thing her kit names on her own side is a friendly MINION, and p.8's waves put six of those on
    /// the board in every configuration.</para>
    ///
    /// <para><b>THE MEASURED FACT THAT SHAPES EVERY JOURNEY OF HERS, and it is content rather than a
    /// staging problem.</b> Milestone 8.35z34 measured the engaged staged board offline before writing
    /// a line of these fixtures: every minion Spawn point inside the Battle Zone is OCCUPIED by its
    /// own miniature at setup. So all nine of her spawn-point faces read their condition as false on
    /// the untouched staging - her Defense values are their plain printed numbers, Dread Razor's
    /// ranged bullet is not bought, and p.19 admits nothing back. Her kit switches on as the wave dies
    /// or once Tide of Darkness is up, which is why the journeys whose subject is a spawn-point clause
    /// live in <see cref="DodgerProgressedUiJourneyPlayModeTests"/> on the Tier III arm, and why the
    /// ones here are about the faces that work without one.</para>
    ///
    /// <para><b>The driving helpers are shared rather than copied.</b> This fixture imports the
    /// hero-agnostic steps the Xargatha fixtures already define - the headless walk, the commitment,
    /// the turn-yielding, the view lookups - because they are about the surface and not about a hero,
    /// and a seventh transcription of them would be a seventh place for the same step to drift. What
    /// is Dodger's own is <see cref="DodgerSeat"/> and the journeys themselves. That those helpers
    /// live on a fixture named for a hero is an observation this milestone records rather than acts
    /// on: extracting them into a shared driver would touch six shipped hero fixtures, which is a
    /// refactor and not a dealing.</para>
    /// </summary>
    public sealed class DodgerUiJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator HeroPiece_StandsOnDodgersOwnBlueSeatAndIsPointedAtByABoardClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;

            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(heroViews, Has.Length.EqualTo(2),
                "This staging deals two heroes - Wasp on Red and Dodger on Blue - so two pieces " +
                "stand and a third would be a stale or duplicated view. Found: " +
                string.Join(", ", heroViews.Select(view => view.Occupancy.HeroId.Value)));

            var dodgerSetup = DodgerSeat(runtime);
            var dodgerView = heroViews.Single(view =>
                view.Occupancy.HeroId.Equals(dodgerSetup.HeroId));

            // Identity: the seat knows which printed set it is, wears the prepared official token
            // rather than the generated prism fallback, and stands on the Blue side.
            Assert.That(dodgerView.HeroContentId.HasValue, Is.True,
                "The Blue piece carries no content identity, so nothing on the board says which " +
                "hero it is.");
            Assert.That(dodgerView.HeroContentId.Value.Value,
                Is.EqualTo(DodgerCardContent.ContentSetId),
                "The content identity is `dodger` and nothing else - the epithet is a display label " +
                "on the seat, and no piece, rule or card is addressed by it.");
            Assert.That(dodgerView.TeamSide, Is.EqualTo(TeamSide.Blue),
                "She takes the Blue seat in this staging, exactly as Brogan, Sabina, Tigerclaw and " +
                "Xargatha do: milestone 8.25 owns the official six-player configuration.");
            Assert.That(dodgerView.HasTokenVisual, Is.True,
                "Dodger's official circular token is prepared, so the piece must be wearing it " +
                "rather than the generated prism the fallback draws.");
            Assert.That(runtime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(dodgerSetup.HeroId)).DisplayName,
                Is.EqualTo("Dodger the Warlock"),
                "The seat carries the rulebook's own setup-step-7 name.");

            // Placement: the map's own first Blue spawn point, and the space the staging put her on.
            Assert.That(dodgerView.ConfiguredSpawnPointId.HasValue, Is.True);
            Assert.That(dodgerView.ConfiguredSpawnPointId.Value.Value, Is.EqualTo("blue-hero-01"),
                "Dodger takes the map's own first Blue spawn point rather than a space invented for " +
                "her; the staging moves the piece and leaves the spawn where p.6 declared it.");
            Assert.That(dodgerView.Coordinate, Is.EqualTo(dodgerSetup.Coordinate));
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(dodgerSetup.HeroId)).Coordinate,
                Is.EqualTo(dodgerView.Coordinate),
                "The drawn piece and the authoritative state must name one space.");

            // The hand the piece is dealt is Dodger's printed set and nobody else's - eighteen faces,
            // because p.20's Handicap card is in the box.
            var dealt = runtime.Cards
                .Where(card => card.OwnerId.Equals(dodgerSetup.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(DodgerCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()));

            // Pointer selection: a camera ray at the piece, past the HUD's own blocking answer.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            if (runtime.ViewedHeroId.Equals(dodgerSetup.HeroId))
            {
                var other = heroViews.First(view =>
                    !view.Occupancy.HeroId.Equals(dodgerSetup.HeroId));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, root.Hud, other.transform.position), Is.True);
                yield return null;
            }

            Assert.That(runtime.ViewedHeroId, Is.Not.EqualTo(dodgerSetup.HeroId),
                "This journey points the tray away first, so the click under test is what brings it " +
                "back rather than something that was already true.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, dodgerView.transform.position), Is.True,
                "The camera ray at her piece must be claimed by the board rather than swallowed.");
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(dodgerSetup.HeroId),
                "Clicking her piece points the tray at her.");

            // Presentation rebuild: the same projection, drawn again, with no stale or duplicated view.
            runtime.RefreshAll();
            yield return null;
            var rebuilt = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(rebuilt, Has.Length.EqualTo(2),
                "A rebuild that leaves a stale or duplicated hero view is the defect this checks.");
            var rebuiltDodger = rebuilt.Single(view =>
                view.Occupancy.HeroId.Equals(dodgerSetup.HeroId));
            Assert.That(rebuiltDodger.Coordinate, Is.EqualTo(dodgerSetup.Coordinate));
            Assert.That(rebuiltDodger.HasTokenVisual, Is.True);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(dodgerSetup.HeroId),
                "The rebuilt tray still follows the hero the player pointed it at.");
        }

        [UnityTest]
        public IEnumerator Dodger_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;

            // She commits the highest-Initiative card in her hand that prints a secondary Movement
            // icon - Dread Razor, at 12 - and the enemy commits their lowest, so she takes the first
            // turn whichever seat the tray opened on. Rulebook p.7 resolves the higher printed
            // Initiative first.
            //
            // Death Trap is the one card of hers this step can never pick, and that is content: it
            // prints no Movement icon at all, which is why its matrix row carries no
            // secondary-movement shape where every other dealt face of hers does.
            yield return CommitEverySeat(root, dodgerId, MovingCardOf(runtime, dodgerId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId),
                "Dodger's higher Initiative gives her p.7's first turn.");
            Assert.That(runtime.GetCommitmentStatus(dodgerId),
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

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(dodgerId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = FirstClickableStep(pointerInput, runtime);
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(dodgerId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(destination, Is.Not.EqualTo(startedAt));

            // The piece follows the state: this is the hero-piece movement half of the row.
            var dodgerView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(dodgerId));
            Assert.That(dodgerView.Coordinate, Is.EqualTo(destination),
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
                .Single(hand => hand.HeroId.Equals(dodgerId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"),
                "Dodger's spent card reads as this round's first turn on her own tray.");
        }

        [UnityTest]
        public IEnumerator TheDirectorDrawer_UndoesDodgersCommitmentAndRestartsHerOntoHerStagedSpace()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;

            var dodgerSetup = DodgerSeat(runtime);
            yield return PointTrayAt(root, dodgerSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, dodgerSetup.HeroId));
            Assert.That(runtime.GetCommitmentStatus(dodgerSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));

            // The drawer relabels its own toggle when open, so it is found by label prefix.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);

            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.GetCommitmentStatus(dodgerSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "The clicked UNDO must retract Dodger's committed card.");

            yield return PointTrayAt(root, dodgerSetup.HeroId);
            yield return CommitThroughTray(
                root, LowestInitiativeCardOf(runtime, dodgerSetup.HeroId));
            UiPlayer.Click(UiPlayer.Button(hud, "RESET"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");
            Assert.That(runtime.GetCommitmentStatus(dodgerSetup.HeroId),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
            var restarted = Object.FindObjectsByType<PrimitiveHeroView>();
            Assert.That(restarted, Has.Length.EqualTo(2),
                "A restart that leaves a duplicate piece behind is what this counts.");
            var dodgerView = restarted.Single(view =>
                view.Occupancy.HeroId.Equals(dodgerSetup.HeroId));
            Assert.That(dodgerView.Coordinate, Is.EqualTo(dodgerSetup.Coordinate),
                "Dodger stands on her configured space again.");
            Assert.That(dodgerView.HasTokenVisual, Is.True,
                "and is still wearing her own official token.");
            Assert.That(runtime.Cards
                    .Count(card => card.OwnerId.Equals(dodgerSetup.HeroId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(DodgerCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        /// <summary>
        /// Dodger's half of milestone 8.20c's standard-footprint conformance: every one of her
        /// verified faces, drawn one at a time on the live HUD panel and measured against
        /// <c>CardFootprint</c>. The check is hero-neutral and enumerates the hero's own content,
        /// which this case supplies.
        ///
        /// <para>Her set is a real test of it rather than a formality. Dread Razor and Middlefinger of
        /// Death each print rulebook p.10's modal form with two bullets on one face, and Darkest
        /// Ritual prints two independent clauses and a parenthesised reminder. A hero added long after
        /// the footprint became a standard has to fit it rather than widen it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsEveryDodgerFaceAtTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();

            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Dodger", DodgerCardContent.AllDefinitions, root.Hud);
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The staged seat dealt Dodger's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState DodgerSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == DodgerCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Dodger to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        /// <summary>
        /// The printed display name of one dealt card instance, upper-cased the way the HUD draws it.
        /// Used where a control is a card ELEMENT rather than a labelled button, which is how the
        /// defense prompt and the hand-card prompt draw their faces.
        /// </summary>
        internal static string NameOfInstance(
            ConfiguredMatchSceneRuntime runtime, CardInstanceId instanceId)
        {
            var card = runtime.Cards.Single(value => value.Id.Equals(instanceId));
            return runtime.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .DisplayName
                .ToUpperInvariant();
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
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Min the Dragonmonk's piece, her tray, her two basic faces and her two Tier I faces, driven
    /// through real pointer events and camera rays. Milestone 8.61k, and the NINETEENTH hero to take
    /// the permanent player-path gate.
    ///
    /// <para><b>THE STAGED BOARD WAS MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, which is the lesson
    /// milestone 8.38f records and the reason the offers below are asserted as exact sets. Milestone
    /// 8.61j's engagement stands her at (-3,1) and the one enemy hero at (-2,2) on a BENT line, with a
    /// friendly Blue melee minion at (-3,2), an enemy Red ranged minion at (-4,2), enemy Red melee
    /// minions at (-1,-1) and (-1,0), a friendly Blue heavy at (0,0), and terrain at (-3,3), (-4,3),
    /// (-3,-2), (-2,-2), (-2,-3), (1,-2) and (-6,2). Her free ring-1 spaces are (-4,1), (-3,0), (-2,0)
    /// and (-2,1); ring 2 adds (-5,1), (-5,2), (-5,3), (-4,0), (-3,-1), (-2,-1) and (-1,1).</para>
    ///
    /// <para><b>Every journey of hers re-stands the two heroes through the Director's own sandbox</b>
    /// rather than taking the engagement as it falls, and the reason is her printed text rather than
    /// convenience: all five of her Stances name a unit <em>adjacent</em> to her and the engagement's
    /// gap is two. <see cref="StageMin"/> therefore puts her at (-3,1) and the enemy at (-2,1) -
    /// measured adjacent - except where a journey's own sentence wants a straight line, and the screen
    /// journey says so where it stands them.</para>
    ///
    /// <para><b>INITIATIVE ORDER IS PART OF EVERY STAGING OF HERS, because her tokens cannot be
    /// staged.</b> A HIGHER printed Initiative acts FIRST, measured on the staged session; her Silver
    /// prints 12 and the enemy's Shock prints 8, so committing Smoke Bomb against Shock is what puts a
    /// bomb on the board before the enemy's turn in the SAME round - which is the only round any bomb
    /// of hers is ever standing (rulebook p.8 removes every token at the end of a round and her card
    /// text says nothing otherwise).</para>
    /// </summary>
    public sealed class MinUiJourneyPlayModeTests
    {
        /// <summary>Where the sandbox stands her: the engagement's own space for the Blue hero.</summary>
        internal static readonly HexCoordinate HerSpace = new HexCoordinate(-3, 1);

        /// <summary>Measured adjacent to <see cref="HerSpace"/> and to the friendly melee minion.</summary>
        internal static readonly HexCoordinate EnemySpace = new HexCoordinate(-2, 1);

        /// <summary>
        /// Two spaces from her along the SAME-R file, so (-2,1) lies strictly between them and MI-Q2's
        /// screen has a space to stand on. Measured: the file through (-3,1) at constant r holds
        /// (-7,1) (-6,1) (-5,1) (-4,1) (-2,1) (-1,1) (0,1) (1,1), every one of them free.
        /// </summary>
        internal static readonly HexCoordinate EnemyOnHerFile = new HexCoordinate(-1, 1);

        /// <summary>
        /// The staged seat dealt Min's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. A staged arm missing from
        /// <c>SelectSetupConfiguration</c> deals her cards to a seat whose content identity is somebody
        /// else's, which is milestone 8.38f's defect and what this assertion exists to catch.
        /// </summary>
        internal static HeroId Min(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == MinCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Min to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0].HeroId;
        }

        /// <summary>Every seat that is not hers, nearest first.</summary>
        internal static HeroId[] Enemies(ConfiguredMatchSceneRuntime runtime)
        {
            var her = Min(runtime);
            return runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(her))
                .Select(hero => hero.HeroId)
                .ToArray();
        }

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the pieces and dealing the hands this journey needs.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the ids
        /// only exist once a configuration has been composed - the shape milestone 8.37a's Guardian
        /// Spirit journey minted and every hero since has reused.</para>
        /// </summary>
        internal static IEnumerator StageMin(
            SceneCardContent content,
            IEnumerable<CardDefinitionId> hand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            HexCoordinate? herSpace = null,
            HexCoordinate? enemySpace = null,
            IEnumerable<KeyValuePair<MinionId, HexCoordinate>> movedMinions = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var her = Min(runtime);
            var enemies = Enemies(runtime);

            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(her, herSpace ?? HerSpace, hand: hand),
                new SandboxHeroStaging(enemies[0], enemySpace ?? EnemySpace, hand: enemyHand)
            };
            for (var index = 1; index < enemies.Length; index++)
            {
                heroes.Add(new SandboxHeroStaging(enemies[index], new HexCoordinate(-5, 1)));
            }

            MatchSceneCompositionRoot.NextComposedSandboxStaging =
                new SandboxStagingDescription(heroes: heroes, movedMinions: movedMinions);
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// Commits her named face out of her own tray and one named card out of every other seat's,
        /// then drives the turn to her and starts the printed action through its own control.
        ///
        /// <para>The enemy's card is NAMED wherever a journey cares about turn ORDER: a higher printed
        /// Initiative acts first, so a journey that needs her token on the board before the enemy moves
        /// or attacks names a card of theirs that prints less than hers.</para>
        /// </summary>
        /// <summary>
        /// Commits her named face and one card from every other seat, through each seat's own tray.
        /// The enemy's card is NAMED wherever a journey cares about turn ORDER, and otherwise the
        /// seat's lowest printed Initiative, which acts last.
        /// </summary>
        internal static IEnumerator CommitEverySeatOfHers(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            CardDefinitionId enemyFace = default)
        {
            var runtime = root.Runtime;
            var her = Min(runtime);
            yield return PointTrayAt(root, her);
            yield return CommitFaceThroughTray(root, NameOf(runtime, face));
            foreach (var seat in Enemies(runtime))
            {
                yield return PointTrayAt(root, seat);
                var named = enemyFace.IsDefined && HandOf(runtime, seat)
                    .Any(definition => definition.Id.Equals(enemyFace));
                yield return CommitFaceThroughTray(
                    root,
                    named ? NameOf(runtime, enemyFace) : LowestInitiativeCardOf(runtime, seat));
            }
        }

        internal static IEnumerator BeginMin(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default)
        {
            var runtime = root.Runtime;
            var her = Min(runtime);
            yield return CommitEverySeatOfHers(root, face, enemyFace);
            yield return WaitForActor(root, her);
            yield return PointTrayAt(root, her);
            yield return StartActionThroughControl(root, kind);
            Healthy(runtime);
        }

        /// <summary>Every token standing on the board, as the authority holds it.</summary>
        internal static PlacedToken[] Tokens(ConfiguredMatchSceneRuntime runtime) =>
            runtime.BoardView.Model.PlacedTokens
                .OrderBy(token => token.Coordinate.Q)
                .ThenBy(token => token.Coordinate.R)
                .ToArray();

        /// <summary>
        /// What one seat is allowed to read off the board about her tokens: the definition ids that
        /// seat's OWN projection carries, which is where milestone 8.61d's facedown mask is applied.
        /// </summary>
        internal static string[] TokenKindsInSeat(
            ConfiguredMatchSceneRuntime runtime, HeroId ownedBy, bool hers)
        {
            var seats = runtime.Host.Seats.Seats
                .Where(seat => seat.Owns(ownedBy) == hers)
                .ToArray();
            Assert.That(seats, Is.Not.Empty,
                (hers ? "Her own seat" : "A seat that is not hers") + " must exist to read from.");
            return seats
                .SelectMany(seat => runtime.Host.ProjectionForSeat(seat.Id).PlacedTokens)
                .Select(token => token.Definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Every seat's own published projection agrees about the board, about whose decision is
        /// standing and about the round - which is the half of a cross-seat prompt no single-seat
        /// journey can see. Her mine trap and both Grenades put a question to a seat that is not the
        /// acting one, and the thing that could silently be wrong about all of them is not whether the
        /// prompt opens but whether every seat is told the same thing about it.
        ///
        /// <para>The token KINDS are deliberately not compared here: they are the one thing the seats
        /// must NOT agree about while a mine is facedown, and <see cref="TokenKindsInSeat"/> is where
        /// that difference is asserted.</para>
        /// </summary>
        internal static void SeatsAgree(ConfiguredMatchSceneRuntime runtime, string moment)
        {
            var seats = runtime.Host.Seats.Seats.ToArray();
            Assert.That(seats, Is.Not.Empty, "A composed match has seats.");
            var readings = seats
                .Select(seat => runtime.Host.ProjectionForSeat(seat.Id))
                .Select(projection =>
                    projection.DecisionOwner + " | " +
                    string.Join(",", projection.Heroes
                        .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                        .Select(hero => hero.HeroId.Value + "@" + hero.Coordinate)) + " | " +
                    projection.RoundIndex + "/" + projection.TurnIndex + " | " +
                    string.Join(",", projection.PlacedTokens
                        .OrderBy(token => token.Id.Value, StringComparer.Ordinal)
                        .Select(token => token.Id.Value + "@" + token.Coordinate)))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(readings, Has.Length.EqualTo(1),
                "Every seat's projection must agree at " + moment + "; they read: " +
                string.Join(" || ", readings));
        }

        /// <summary>
        /// Her own tray card, addressed by the face it prints rather than by any text that face
        /// MENTIONS. Milestone 8.61k, and it is her problem specifically: six of her faces print the
        /// words "Smoke bomb" in their rules text, and the shared
        /// <c>UiPlayer.NamedElementWithText</c> matches a tray card's assistive tooltip by SUBSTRING -
        /// so committing "SMOKE BOMB" out of a hand also holding Poof! finds two cards and fails.
        /// The tooltip is composed as "{DisplayName} - {ColorName} - ..."
        /// (<c>TrayCardViewModel.AssistiveLabel</c>), so the face is named by what the tooltip STARTS
        /// with, which is the card and never the sentence.
        /// </summary>
        internal static VisualElement TrayFace(ConfiguredMatchHudRenderer hud, string printedName)
        {
            var matches = UiPlayer.Descendants(hud.RootElement)
                .Where(candidate => candidate.name == "gd-card-choice" &&
                    UiPlayer.IsShown(candidate) &&
                    UiPlayer.Descendants(candidate).Prepend(candidate).Any(element =>
                        !string.IsNullOrEmpty(element.tooltip) &&
                        element.tooltip.StartsWith(
                            printedName, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                "Expected exactly one shown tray card whose face is '" + printedName +
                "'; the shown controls are: " + UiPlayer.Describe(hud));
            return matches[0];
        }

        /// <summary>Commits one named face out of the viewed seat's own tray, through p.7's control.</summary>
        internal static IEnumerator CommitFaceThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            UiPlayer.EnsureVisible(TrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(TrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        /// <summary>
        /// Presses the numeric control a printed distance offers - "up to 3 spaces" is drawn as the
        /// integers nought to three on the decision window, and an integer candidate stands on no space
        /// at all, so the shared board-clicking Pick would aim a camera ray at the origin.
        /// </summary>
        internal static IEnumerator PressAmount(MatchSceneCompositionRoot root, int amount)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "No distance is standing to press.");
            var node = choice.NodeId;
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Integer &&
                command.EffectChoice.Candidate.Value.IntegerValue == amount);
            Assert.That(control, Is.Not.Null,
                "The decision window offers no control for " + amount + " on '" + node.Value +
                "'. The shown controls are: " + UiPlayer.Describe(root.Hud));
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
            yield return null;
            Healthy(runtime);
        }

        /// <summary>
        /// One filler decision, taken the way <c>NextRoundDecision</c> takes one - except that a
        /// COMMITMENT is submitted through the projected command rather than through the tray.
        ///
        /// <para>Milestone 8.61k, and it is her problem specifically: the shared filler commits
        /// <c>hand.Cards[0]</c> by its printed name, and six of her faces print the words "Smoke bomb"
        /// in their rules text, so a filler round in a hand holding both her Silver and any vanishing
        /// Block finds two tray cards and stops. The tray is the subject of her own spawn journey and
        /// of every journey that starts an action; here it is filler, and the same split milestone
        /// 8.15 established for the commitment applies.</para>
        /// </summary>
        internal static IEnumerator NextDecision(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            // The projection's own commitment ledger rather than the viewed seat's tray: a hand
            // the tray is not currently pointed at is still a seat p.7 is waiting for, and the
            // shared filler's tray-shaped reading is what stalls on a round where the awaiting seat
            // is somebody else's.
            var awaiting = runtime.Projection.Commitments
                .FirstOrDefault(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            var held = !awaiting.HeroId.IsDefined
                ? null
                : runtime.Cards.FirstOrDefault(card => card.OwnerId.Equals(awaiting.HeroId) &&
                    card.State.Location == CardLocation.Hand);
            if (held != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.CommitCard(awaiting.HeroId, held.Id)),
                    Is.True,
                    "A seat awaiting p.7's commitment can always commit a card it holds. " +
                    Remaining(runtime));
                yield return null;
                yield break;
            }

            if (awaiting.HeroId.IsDefined)
            {
                // A seat that has run out of cards cannot commit one, and p.7's own answer for that
                // is to pass. The shared filler has no branch for it, which is what stalls a journey
                // whose staged hands are deliberately short.
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.Pass(awaiting.HeroId)),
                    Is.True,
                    "A seat holding nothing can always pass. " + Remaining(runtime));
                yield return null;
                yield break;
            }

            yield return NextRoundDecision(root);
        }

        /// <summary>Drives whole rounds until the predicate holds, or fails naming what stood.</summary>
        internal static IEnumerator DriveUntil(
            MatchSceneCompositionRoot root, Func<bool> until, string what, int budget = 320)
        {
            for (var step = 0; step < budget && !until(); step++)
            {
                yield return NextDecision(root);
            }

            Assert.That(until(), Is.True,
                "The filler never reached " + what + ". " + Remaining(root.Runtime));
        }

        /// <summary>One card of this hero's, by the face it prints - the handle an Active effect is read by.</summary>
        internal static CardInstanceId CardIdOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definition) =>
            runtime.Cards
                .Single(card => card.OwnerId.Equals(heroId) &&
                    card.DefinitionReference.DefinitionId.Equals(definition))
                .Id;

        /// <summary>Every card of this hero's currently in the discard pile, as definition ids.</summary>
        internal static string[] DiscardsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        // -------------------------------------------------------------------------------------------
        // The component half her piece owns, on her own dealt board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her own token stands at the configured Blue spawn as Min and only once, a camera ray on it
        /// points the tray at her, and her tray commits a face through rulebook p.7's own reveal.
        /// </summary>
        [UnityTest]
        public IEnumerator HerPieceSpawnsAsMinAndHerTrayCommitsAndRevealsACard()
        {
            // Her dealt five hold Poof!, whose printed text NAMES the Smoke bomb; the staged hand
            // leaves it out so the tray holds exactly one card whose face is Smoke Bomb.
            yield return StageMin(
                SceneCardContent.VerifiedEngagedStagedMin,
                hand: new[]
                {
                    MinCardContent.SmokeBomb, MinCardContent.CraneStance, MinCardContent.TripMine,
                    MinCardContent.FastAsLightning
                });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);

            var pieces = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                .Where(view => view.Occupancy.HeroId.Equals(her))
                .ToArray();
            Assert.That(pieces, Has.Length.EqualTo(1),
                "One hero, one piece - a second would be a view the rebuild failed to retire.");
            Assert.That(pieces[0].Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(her)).Coordinate),
                "and it stands where the authority says she does.");
            Assert.That(
                runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(her)).SpawnPoint.ToString(),
                Does.Contain("blue-hero-01"),
                "The staged engagement preserves her configured spawn point, which is what p.13's " +
                "respawn later returns her to.");

            yield return PointTrayAt(root, her);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(her),
                "A camera ray on her own piece points the tray at her.");

            yield return CommitFaceThroughTray(root, NameOf(runtime, MinCardContent.SmokeBomb));
            Assert.That(runtime.GetCommitmentStatus(her),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown).Or
                    .EqualTo(TurnCardCommitmentStatus.Revealed),
                "CONFIRM FACEDOWN puts her card in p.7's turn area. " + Remaining(runtime));
            foreach (var seat in Enemies(runtime))
            {
                yield return PointTrayAt(root, seat);
                yield return CommitFaceThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }

            Assert.That(runtime.GetCommitmentStatus(her),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed),
                "and the reveal follows the last seat's commitment. " + Remaining(runtime));
            SeatsAgree(runtime, "the reveal");
            Healthy(runtime);
        }

        /// <summary>
        /// Every face she prints that carries rulebook p.10's secondary Movement ribbon walks the board
        /// through its own card face, and every face she prints that carries p.14's secondary Defense
        /// shield is offered in the defender's own prompt.
        ///
        /// <para>MI-05 splits her set twice over and both halves are measured here rather than assumed.
        /// <b>Smoke Bomb prints a Defense 1 and NO Movement ribbon at all</b>, so the walk half skips
        /// it; <b>Poof!, Vanish and Ruse print a Movement 3 and no Defense value whatsoever</b> - they
        /// are Block cards whose only defense is the Block, Tigerclaw's Dodge precedent - so the defend
        /// half skips those three and their own journeys answer p.14 with them instead. Flurry of Blows
        /// prints no action at all and cannot be committed, walked or discarded to defend.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in MinCardContent.DealtDefinitions
                .Where(card => card.ActionDefinitions.Count > 0))
            {
                var walks = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary);
                var defends = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction);

                if (walks)
                {
                    yield return StageMin(
                        SceneCardContent.VerifiedEngagedStagedMin,
                        hand: new[] { definition.Id, MinCardContent.SmokeBomb });
                    var root = Root();
                    var runtime = root.Runtime;
                    var her = Min(runtime);
                    yield return BeginMin(root, definition.Id, ActionKind.Movement);
                    Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                        definition.DisplayName + "'s printed boot opens a walk. " + Remaining(runtime));
                    var step = FirstClickableSpace(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        runtime.PendingMovementChoice.LegalDestinations
                            .Where(value => value.Cost == 1)
                            .Select(value => value.Destination)
                            .ToArray());
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            Tile(step).transform.position),
                        Is.True,
                        definition.DisplayName + " must accept a board click on a lit step.");
                    yield return null;
                    Assert.That(Position(runtime, her), Is.EqualTo(step),
                        "and her piece follows the authoritative space. " + Remaining(runtime));
                    Assert.That(HeroView(her).Coordinate, Is.EqualTo(step));
                    Healthy(runtime);
                }

                if (!defends)
                {
                    continue;
                }

                var committed = definition.Id.Equals(MinCardContent.SmokeBomb)
                    ? MinCardContent.CraneStance
                    : MinCardContent.SmokeBomb;
                yield return StageMin(
                    SceneCardContent.VerifiedEngagedStagedMin,
                    hand: new[] { definition.Id, committed },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var defenseRoot = Root();
                var defenseRuntime = defenseRoot.Runtime;
                var defender = Min(defenseRuntime);
                var enemy = Enemies(defenseRuntime)[0];
                yield return PointTrayAt(defenseRoot, defender);
                yield return CommitFaceThroughTray(
                    defenseRoot, NameOf(defenseRuntime, committed));
                yield return PointTrayAt(defenseRoot, enemy);
                yield return CommitFaceThroughTray(
                    defenseRoot, NameOf(defenseRuntime, WaspCardContent.Shock));
                yield return YieldUntilTheNamedSeatActs(defenseRuntime, enemy);
                yield return PointTrayAt(defenseRoot, enemy);
                yield return StartActionThroughControl(defenseRoot, ActionKind.Attack);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        defenseRoot.Hud,
                        HeroView(defender).transform.position),
                    Is.True);
                yield return null;
                Assert.That(
                    defenseRuntime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                        offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)),
                    Is.True,
                    definition.DisplayName + " is offered as a p.14 Defense. " +
                    Remaining(defenseRuntime));
                yield return ClickDefenseFace(defenseRoot, definition.Id);
                Assert.That(defenseRuntime.PendingDefenseChoice, Is.Null);
                Assert.That(
                    defenseRuntime.Cards.Single(card =>
                        card.OwnerId.Equals(defender) &&
                        card.DefinitionReference.DefinitionId.Equals(definition.Id))
                        .State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "and answering with it spends it into her own discard pile.");
                Healthy(defenseRuntime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Silver: the Smoke bomb and MI-Q2's screen
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Smoke Bomb: "Place a Smoke bomb token in radius; enemy heroes cannot target you or another
        /// unit if the smoke bomb token is on a straight line between that enemy hero and their
        /// target."
        ///
        /// <para><b>Both halves are driven in ONE round, and they have to be.</b> Rulebook p.8 removes
        /// every token at the end of a round and her card text says nothing otherwise, so the only
        /// window in which a bomb of hers exists is the round her Silver resolved in. Her Silver prints
        /// Initiative 12 and the enemy's Lift Up prints 10, and a higher Initiative acts first, so the
        /// bomb is standing when the enemy's own card-text selection opens.</para>
        ///
        /// <para><b>The enemy's card is Lift Up, and the first two Unity runs of this journey are why.</b>
        /// MI-Q2 makes the screen every p.19 Target at once - attacks, pushes, swaps and card-text unit
        /// choices - and the obvious probe, a ranged Attack, cannot be used against Wasp at all: BOTH
        /// of her printed Ranged Attacks say "in range and NOT IN A STRAIGHT LINE"
        /// (<c>StraightLineQuery.FilterOutStraightLine</c>), so her own text already excludes exactly
        /// the targets a screen would. Lift Up prints "Move a unit, or a token, in radius 1 space" with
        /// no such qualifier, so what changes between the two runs below is the bomb and nothing
        /// else.</para>
        ///
        /// <para><b>The screen is proved by a PAIR of runs on one measured file.</b> She stands at
        /// (-3,1) and the enemy at (-1,1) - the same-r file, two apart and inside Lift Up's printed
        /// Radius 2 - so (-2,1) is the one space strictly between them. With the bomb there the enemy's
        /// selection does not offer her at all; with the bomb one space off that file at (-2,0) the
        /// same selection offers her. A single run could not tell a working screen from a card that
        /// never reached her.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SmokeBombPlacesHerTokenAndScreensTheEnemyTargetingAcrossItsStraightLine()
        {
            // MEASURED at 8.61k: the off-line bomb stands at (-4,1), BEHIND her rather than beside
            // her. Lift Up moves its subject without changing that subject's distance from Wasp, so a
            // bomb on (-2,0) would take away the one space Min could legally be moved to and she
            // would fall out of the offer for a reason that is not the screen at all - which is what
            // the second Unity run of this journey measured.
            foreach (var bombAt in new[] { new HexCoordinate(-2, 1), new HexCoordinate(-4, 1) })
            {
                var screens = bombAt.Equals(new HexCoordinate(-2, 1));
                yield return StageMin(
                    SceneCardContent.VerifiedEngagedStagedMin,
                    hand: new[] { MinCardContent.SmokeBomb, MinCardContent.Poof },
                    enemyHand: new[] { WaspCardContent.LiftUp, WaspCardContent.Electrocute },
                    enemySpace: EnemyOnHerFile);
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(Tokens(runtime), Is.Empty, "Arrange: nothing is on the board yet.");

                yield return BeginMin(
                    root, MinCardContent.SmokeBomb, ActionKind.Skill, WaspCardContent.LiftUp);
                var place = runtime.PendingEffectChoice;
                Assert.That(place, Is.Not.Null,
                    "Her Silver's Place asks for a space. " + Remaining(runtime));
                Assert.That(place.NodeId.Value, Is.EqualTo("from-supply"),
                    "With no bomb of hers standing, the branch draws one from the supply rather " +
                    "than relocating.");
                Assert.That(
                    OfferedSpaces(runtime).All(space =>
                        Distance(space, Position(runtime, her)) <= 3),
                    Is.True,
                    "Every candidate is inside her printed Radius 3. " + Remaining(runtime));
                Assert.That(OfferedSpaces(runtime), Contains.Item(bombAt));
                yield return Space(root, bombAt);

                var token = Tokens(runtime).Single();
                Assert.That(token.Definition.Id, Is.EqualTo(MinComponentContent.SmokeBomb));
                Assert.That(token.Coordinate, Is.EqualTo(bombAt),
                    "and the piece stands on the space she clicked.");
                Assert.That(runtime.BoardView.TokenViews.Count(view => view.gameObject.activeSelf),
                    Is.EqualTo(1),
                    "The board draws exactly one token for it.");
                Assert.That(TokenKindsInSeat(runtime, her, hers: false),
                    Is.EqualTo(new[] { MinComponentContent.SmokeBomb.Value }).AsCollection,
                    "The bomb is not facedown, so every other seat reads it for what it is - which " +
                    "is the negative that keeps the mines' own mask meaningful.");
                SeatsAgree(runtime, "the bomb placed");

                // The enemy's own turn, later in the SAME round, and their own card-text selection.
                yield return YieldUntilTheNamedSeatActs(runtime, enemy);
                yield return PointTrayAt(root, enemy);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                var offered = runtime.PendingEffectChoice;
                Assert.That(offered, Is.Not.Null,
                    "Lift Up's printed Radius 2 opens its own unit choice. " + Remaining(runtime));
                Assert.That(Distance(Position(runtime, her), Position(runtime, enemy)),
                    Is.EqualTo(2),
                    "Arrange: two apart on one file, which is the only geometry p.19's straight " +
                    "line admits with a space strictly between.");
                var reading = "bomb at " + bombAt + ", she is at " + Position(runtime, her) +
                    ", he is at " + Position(runtime, enemy) + ", offered [" + string.Join(", ",
                        offered.Candidates.Select(candidate => candidate.Value.Kind + ":" +
                            (candidate.Value.Kind == EffectValueKind.Hero
                                ? candidate.Value.HeroId.Value
                                : candidate.Value.Kind == EffectValueKind.Minion
                                    ? candidate.Value.MinionId.Value
                                    : candidate.Value.Kind.ToString()))) + "]. ";
                // Lift Up publishes its subjects as COORDINATES - "a unit, or a token" - so the
                // hero it may move is named by the space she stands on, which is what the first two
                // runs of this journey measured.
                Assert.That(
                    offered.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Coordinate &&
                        candidate.Value.Coordinate.Equals(Position(runtime, her))),
                    Is.EqualTo(!screens),
                    reading + (screens
                        ? "The bomb lies strictly between them on the same-r file, so this enemy " +
                          "hero cannot target her at all and the control never offers her. "
                        : "One space off that file screens nothing, so the same control offers her " +
                          "exactly as it did before any bomb stood. ") + Remaining(runtime));
                Assert.That(offered.Candidates, Is.Not.Empty,
                    "and the screen narrows the offer rather than emptying it - his own minions " +
                    "and her own bomb are untouched by a rule about HIS targeting of HER side. " +
                    reading);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Gold: the borrowed after-attack rider
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Fast as Lightning: "Target a unit in range. After the attack: Apply the 'After the attack'
        /// text of your resolved or discarded red card. (If it has radius, use that card's value)."
        ///
        /// <para><b>Both halves of MI-Q7 are driven.</b> A staged hand naming only Fast as Lightning
        /// discards her other four dealt cards, so Crane Stance is in the pile and the borrow's pool
        /// holds exactly it - the control opens, the face is picked on its whole card, and Crane's own
        /// rider then runs inside this attack's frame with its push. A staged hand naming all five
        /// leaves the pile empty and every Red of hers in HAND, and the clause then offers nothing at
        /// all: "resolved or discarded" is not "held".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FastAsLightningBorrowsHerRedCardsAfterTheAttackTextAndOffersNothingWithoutOne()
        {
            foreach (var withARedInThePile in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedEngagedStagedMin,
                    hand: withARedInThePile
                        ? new[] { MinCardContent.FastAsLightning }
                        : MinCardContent.StartingHand.ToArray(),
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(
                    DiscardsOf(runtime, her).Contains(MinCardContent.CraneStance.Value),
                    Is.EqualTo(withARedInThePile),
                    "Arrange: a staged hand that does not name a dealt card discards it.");

                yield return BeginMin(
                    root, MinCardContent.FastAsLightning, ActionKind.Attack, WaspCardContent.Shock);
                Assert.That(
                    runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "MI-05: her Gold prints Range 1 under the Ranged band, so its reach IS " +
                    "adjacency and the sandbox stands exactly one enemy hero there. " +
                    Remaining(runtime));
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;

                if (!withARedInThePile)
                {
                    // MILESTONE 8.86, SO THIS ARM IS NOT AN ABSENCE. The silence is read as the
                    // printed rule only after the Attack it hangs off is shown to have happened and
                    // the pool it reads is shown to be empty for the reason MI-Q7 gives.
                    Assert.That(
                        runtime.Cards.Single(card =>
                            card.OwnerId.Equals(enemy) &&
                            card.DefinitionReference.DefinitionId.Equals(
                                WaspCardContent.Electrocute)).State.Location,
                        Is.EqualTo(CardLocation.DiscardPile),
                        "Her Gold's own Attack reached p.14 and the defender spent the face they " +
                        "answered with. " + Remaining(runtime));
                    Assert.That(
                        DiscardsOf(runtime, her).Contains(MinCardContent.CraneStance.Value),
                        Is.False,
                        "and her one Red is still in HAND rather than in the pile, which is the " +
                        "whole of why the borrow names nothing. " + Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "MI-Q7: a Red still in hand is neither resolved nor discarded, so the " +
                        "borrow has no card to apply and draws no control. " + Remaining(runtime));
                    Healthy(runtime);
                    continue;
                }

                // MEASURED at milestone 8.61k: on her real set the borrow's pool holds AT MOST ONE
                // card, because only p.6's dealt five can be discarded by a staging and only one of
                // them is Red - so p.10's decision is designated rather than undesignated and the
                // resolver binds it without drawing a control. The matrix's own classification of
                // `borrow-after-attack@1` says exactly that: the whole-face selection is what the
                // capability ASKS, and "the control is drawn only where p.10's undesignated decision
                // genuinely opens". What is asserted here is therefore the binding and its
                // consequence, and the pool is read off the discard pile rather than off a prompt.
                Assert.That(
                    DiscardsOf(runtime, her).Count(value =>
                        value == MinCardContent.CraneStance.Value),
                    Is.EqualTo(1),
                    "Her one discarded Red is the whole of the borrow's pool. " +
                    Remaining(runtime));

                var subject = runtime.PendingEffectChoice;
                Assert.That(subject, Is.Not.Null,
                    "and Crane Stance's own rider runs inside THIS attack's frame, bound to the " +
                    "one Red the pool holds. " + Remaining(runtime));
                Assert.That(subject.NodeId.Value, Is.EqualTo("crane-subject"),
                    "which is the borrowed sentence and not one of Fast as Lightning's own. " +
                    Remaining(runtime));
                Assert.That(subject.ChooserId, Is.EqualTo(her));
                Assert.That(
                    subject.Candidates.All(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero ||
                        candidate.Value.Kind == EffectValueKind.Minion),
                    Is.True,
                    "\"Push an enemy unit adjacent to you\" is a piece on the board.");
                Assert.That(subject.Candidates, Is.Not.Empty, Remaining(runtime));

                // MILESTONE 8.86, THE HALF THIS JOURNEY STOPPED SHORT OF. It asserted that the
                // borrowed sentence OPENED and left it standing, so a borrow that reached the right
                // node and then pushed nobody would have passed. The borrowed clause is now driven
                // to its end - a named subject, a non-zero distance, and the board read afterwards -
                // because "apply the text of your resolved or discarded red card" is a promise about
                // what the other card DOES.
                var pushed = subject.Candidates
                    .OrderBy(candidate => candidate.Value.Kind == EffectValueKind.Hero ? 0 : 1)
                    .ThenBy(candidate => UnitName(candidate.Value), StringComparer.Ordinal)
                    .First();
                var pushedUnit = pushed.Value;
                var pushedFrom = SpaceOf(runtime, pushedUnit);
                yield return Pick(root, pushed);

                var distance = runtime.PendingEffectChoice;
                Assert.That(distance, Is.Not.Null,
                    "Crane Stance's own \"up to 3 spaces\" is its own numeric control, and the " +
                    "borrow runs the whole borrowed sentence. " + Remaining(runtime));
                Assert.That(distance.NodeId.Value, Is.EqualTo("crane-push"));
                var amounts = distance.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Integer)
                    .Select(candidate => candidate.Value.IntegerValue)
                    .OrderBy(value => value)
                    .ToArray();
                Assert.That(amounts, Is.Not.Empty, Remaining(runtime));
                Assert.That(amounts.All(value => value >= 0 && value <= 3), Is.True,
                    "MI-Q7: \"(If it has radius, use that card's value)\" - the borrowed card's own " +
                    "printed \"up to 3\" is the ceiling, not her Gold's. Offered: " +
                    string.Join(", ", amounts) + ". " + Remaining(runtime));
                // A NON-ZERO answer, deliberately: a journey may not prove a push by declining it.
                var chosenAmount = amounts.Where(value => value > 0).DefaultIfEmpty(0).Max();
                Assert.That(chosenAmount, Is.GreaterThan(0),
                    "and the board admits a push of at least one space, which is what makes the " +
                    "reading below a measurement. " + Remaining(runtime));
                yield return PressAmount(root, chosenAmount);
                Assert.That(
                    Distance(SpaceOf(runtime, pushedUnit), pushedFrom), Is.EqualTo(chosenAmount),
                    "THE ASSERTION THE BORROW WAS MISSING: the borrowed push really moved the unit " +
                    "it named, exactly as far as was pressed. " + Remaining(runtime));
                Assert.That(
                    Distance(SpaceOf(runtime, pushedUnit), Position(runtime, her)),
                    Is.EqualTo(Distance(pushedFrom, Position(runtime, her)) + chosenAmount),
                    "and p.18 pushes in a straight line directly away from her. " +
                    Remaining(runtime));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Red Tier I: Crane Stance
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Crane Stance: "Target a unit adjacent to you. After the attack: Push an enemy unit adjacent
        /// to you up to 3 spaces."
        ///
        /// <para><b>The printed "up to 3" is a control the player presses</b>, which is milestone
        /// 8.39j's generalisation: the push reads its own two literals and hands over the integers
        /// nought to three when they differ. Her three pushing Stances are the first content of hers to
        /// reach it, and the journey presses a middle answer rather than the maximum, so a push that
        /// silently took its ceiling would fail here.</para>
        ///
        /// <para>The subject offer is asserted as an exact set: the enemy hero at (-2,1) and the enemy
        /// Red ranged minion at (-4,2) are the two enemy units the measured staging leaves beside her,
        /// and the friendly Blue melee minion at (-3,2) is not one of them - "an ENEMY unit".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CraneStancePushesAnAdjacentEnemyTheDistanceSheChooses()
        {
            yield return StageMin(
                SceneCardContent.VerifiedEngagedStagedMin,
                hand: new[] { MinCardContent.CraneStance, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.CraneStance, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "\"a unit adjacent to you\" - the one enemy hero the sandbox stands beside her. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;

            // He survives on his own printed shield, or the push would have no subject to name.
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                "Arrange: Electrocute's printed Defense 6 answers her printed Attack 3.");

            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null,
                "\"After the attack\" stops the page with her push. " + Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("crane-subject"));
            Assert.That(
                subject.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId)
                    .ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection);
            yield return Pick(
                root,
                subject.Candidates.Single(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero));

            var distance = runtime.PendingEffectChoice;
            Assert.That(distance, Is.Not.Null,
                "and \"up to 3 spaces\" is its own numeric control. " + Remaining(runtime));
            Assert.That(distance.NodeId.Value, Is.EqualTo("crane-push"));
            Assert.That(
                distance.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Integer)
                    .Select(candidate => candidate.Value.IntegerValue)
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(new[] { 0, 1, 2, 3 }).AsCollection,
                "Nought through three, which is what the printed \"up to\" means. " +
                Remaining(runtime));

            var before = Position(runtime, enemy);
            yield return PressAmount(root, 2);
            Assert.That(Distance(Position(runtime, enemy), before), Is.EqualTo(2),
                "Two spaces, because two is what she pressed. " + Remaining(runtime));
            Assert.That(
                Distance(Position(runtime, enemy), Position(runtime, her)),
                Is.EqualTo(Distance(before, Position(runtime, her)) + 2),
                "and p.18 pushes in a straight line directly away from her.");
            Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(Position(runtime, enemy)));
            SeatsAgree(runtime, "the push");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Her Blue Tier I: the facedown mines and the trap they set
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Trip Mine: "Place 2 mine tokens, 1 blast and 1 dud, facedown in radius; units may move
        /// through them."
        ///
        /// <para><b>This is the journey milestone 8.61j's new shape exists to force.</b> The placement
        /// itself is two ordinary board clicks inside her printed Radius 3, in the printed order -
        /// blast first, dud second. What is not ordinary is what every OTHER seat is then shown: the
        /// authority keeps both kinds, her own seat reads them, and every seat that is not hers reads
        /// <c>min-mine-facedown</c> twice. A journey driven from her seat alone could not tell a
        /// working mask from no mask at all.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TripMinePlacesTwoFacedownMinesNoOtherSeatCanIdentify()
        {
            yield return StageMin(
                SceneCardContent.VerifiedEngagedStagedMin,
                hand: new[] { MinCardContent.TripMine, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);

            yield return BeginMin(
                root, MinCardContent.TripMine, ActionKind.Skill, WaspCardContent.Shock);

            var blast = runtime.PendingEffectChoice;
            Assert.That(blast, Is.Not.Null, "Her Blue's first Place. " + Remaining(runtime));
            Assert.That(blast.NodeId.Value, Is.EqualTo("place-mine-0"),
                "MI-Q10d: the PRINTED order decides which copy happens first, and every mine face " +
                "of hers prints its blasts first.");
            Assert.That(
                OfferedSpaces(runtime).All(space =>
                    Distance(space, Position(runtime, her)) <= 3),
                Is.True,
                "Every candidate is inside her printed Radius 3. " + Remaining(runtime));
            yield return Space(root, new HexCoordinate(-2, 0));

            var dud = runtime.PendingEffectChoice;
            Assert.That(dud, Is.Not.Null,
                "and the second copy is asked for separately. " + Remaining(runtime));
            Assert.That(dud.NodeId.Value, Is.EqualTo("place-mine-1"));
            Assert.That(OfferedSpaces(runtime).Contains(new HexCoordinate(-2, 0)), Is.False,
                "The first mine's space is occupied by a piece now, so the second cannot take it.");
            yield return Space(root, new HexCoordinate(-3, 0));

            var standing = Tokens(runtime);
            Assert.That(standing, Has.Length.EqualTo(2));
            Assert.That(
                standing.Select(token => token.Coordinate).ToArray(),
                Is.EquivalentTo(new[] { new HexCoordinate(-3, 0), new HexCoordinate(-2, 0) }));
            Assert.That(runtime.BoardView.TokenViews.Count(view => view.gameObject.activeSelf),
                Is.EqualTo(2),
                "and the board draws a piece for each.");

            Assert.That(
                TokenKindsInSeat(runtime, her, hers: true),
                Is.EqualTo(new[]
                {
                    MinComponentContent.MineBlast.Value, MinComponentContent.MineDud.Value
                }.OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                "The hero who placed them knows which is which. " + Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: false),
                Is.EqualTo(new[]
                {
                    MinComponentContent.MineFacedown.Value, MinComponentContent.MineFacedown.Value
                }).AsCollection,
                "p.4: a facedown component's kind is hidden from every other seat, teammates " +
                "included - and the piece, its space and its supply all still reach the table. " +
                Remaining(runtime));
            SeatsAgree(runtime, "two mines facedown");
            Healthy(runtime);
        }

        /// <summary>
        /// Trip Mine's second sentence: "After an enemy hero moves through a mine, remove it; for every
        /// blast removed, that hero discards a card, if able."
        ///
        /// <para><b>Her first cross-seat prompt, and the one a single-seat journey cannot tell from a
        /// working one.</b> The discard's chooser is the hero who walked over the mine, so
        /// <c>EffectChoiceRequest.ChooserHeroId</c> names the ENEMY - the field
        /// <c>SeatProjection.OwnerOfStandingDecision</c> reads - and a chooser that fell back to the
        /// actor would look exactly like a working prompt from Min's side of the table. The journey
        /// therefore drives the enemy's own Movement on the enemy's own seat, asserts that Min is
        /// refused as the wrong chooser, answers it as the enemy, and reads every seat's projection
        /// afterwards.</para>
        ///
        /// <para><b>The route is forced rather than hoped for</b>, which is MI-Q3's measured reading
        /// that a one-click destination prefers an equal-cost route avoiding mines. (-2,1) and (-2,-1)
        /// share exactly ONE neighbour - (-2,0) - because (-2,-2) is terrain and (-1,-1) is occupied,
        /// so the blast stands on the only two-cost route between them.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AnEnemyMovementThroughHerBlastRevealsItAndTakesThatHerosOwnCard()
        {
            yield return StageMin(
                SceneCardContent.VerifiedEngagedStagedMin,
                hand: new[] { MinCardContent.TripMine, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.TripMine, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-2, 0));
            yield return Space(root, new HexCoordinate(-3, 0));
            Assert.That(Tokens(runtime), Has.Length.EqualTo(2), Remaining(runtime));
            var enemyHandBefore = HandCountOf(runtime, enemy);

            // The enemy's own turn, later in the same round, walked on the enemy's own seat.
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "Shock's printed Movement 4 opens his walk. " + Remaining(runtime));
            var walkable = runtime.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(walkable.Contains(new HexCoordinate(-2, 0)), Is.False,
                "p.18: a unit may move THROUGH a mine and may never end its movement on one. " +
                Remaining(runtime));
            Assert.That(walkable, Contains.Item(new HexCoordinate(-2, -1)));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(new HexCoordinate(-2, -1)).transform.position),
                Is.True,
                "and the one two-cost route to it crosses the blast.");
            yield return null;
            yield return null;
            Assert.That(Position(runtime, enemy), Is.EqualTo(new HexCoordinate(-2, -1)));

            // MEASURED at 8.61k: "after an enemy hero moves through a mine" fires when the whole
            // MOVEMENT ACTION ends, not when a destination is reached - a hero with movement left
            // is still choosing, and the trap has not sprung. So the walk is finished the way a
            // player finishes one, on the control the round strip draws for it.
            UiPlayer.Click(
                UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return null;

            Assert.That(
                Tokens(runtime).Select(token => token.Coordinate).ToArray(),
                Is.EqualTo(new[] { new HexCoordinate(-3, 0) }).AsCollection,
                "\"remove it\" - the blast is gone and the dud she never crossed is not. " +
                Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: false),
                Is.EqualTo(new[] { MinComponentContent.MineFacedown.Value }).AsCollection,
                "and the one still standing is still facedown to him.");

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null,
                "\"that hero discards a card\" stops the action it interrupted. " +
                Remaining(runtime));
            Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                "It is the CROSSING hero's decision, on their own seat - Min set the mine and does " +
                "not choose which card it costs. " + Remaining(runtime));
            Assert.That(
                discard.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemy)),
                Is.True,
                "and the faces offered are his own hand.");
            SeatsAgree(runtime, "the enemy's own mine discard");

            var wrongChooser = DirectorCommand.AnswerEffectChoice(
                new EffectChoiceCommand(
                    discard.ResolutionId, discard.ChoiceId, EffectChoiceCommandKind.Select,
                    discard.Candidates[0], her));
            Assert.That(runtime.SubmitProjectedCommand(wrongChooser), Is.False,
                "Min is refused the answer outright, which is the half a journey driven from her " +
                "seat alone would never see. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the refusal leaves the question exactly where it was.");

            yield return Pick(root, Named(discard.Candidates, "discard"));
            Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(enemyHandBefore - 1),
                "One blast, one card, out of that hero's own hand. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            SeatsAgree(runtime, "the mine trap answered");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Her Green Tier I: the vanishing Block
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Poof!: "Swap with a Smoke bomb token in range; if you do, block the attack."
        ///
        /// <para><b>MI-Q8's two outcomes, both driven through the defender's own prompt.</b> With her
        /// bomb standing inside the card's printed Range 3 the swap is mandatory and the attack is
        /// blocked regardless of its value; with no bomb on the board at all the Block's condition
        /// fails, and Poof! prints NO Defense value to fall back on, so p.14 defeats her.</para>
        ///
        /// <para>The bomb has to be placed in the SAME round as the defence - p.8 removes it at the
        /// round's end - so the positive run commits her Silver at Initiative 12 against the enemy's
        /// Shock at 8, and the attack that Poof! answers is the one Shock opens on the enemy's own
        /// turn.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PoofSwapsWithHerSmokeBombToBlockAndIsDefeatedWithNoneInRange()
        {
            foreach (var withABomb in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedEngagedStagedMin,
                    hand: new[]
                    {
                        withABomb ? MinCardContent.SmokeBomb : MinCardContent.CraneStance,
                        MinCardContent.Poof
                    },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];

                var bombAt = new HexCoordinate(-4, 1);
                if (withABomb)
                {
                    yield return BeginMin(
                        root, MinCardContent.SmokeBomb, ActionKind.Skill, WaspCardContent.Shock);
                    yield return Space(root, bombAt);
                    Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(bombAt),
                        "Arrange: her one bomb, one space from her and well inside Poof!'s " +
                        "printed Range 3. " + Remaining(runtime));
                }
                else
                {
                    // No action at all: END TURN with an action already started would CANCEL that
                    // action rather than end the turn, which is milestone 8.35p's wall.
                    yield return CommitEverySeatOfHers(
                        root, MinCardContent.CraneStance, WaspCardContent.Shock);
                    yield return WaitForActor(root, her);
                    yield return PointTrayAt(root, her);
                    UiPlayer.Click(
                        UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                    yield return null;
                    yield return null;
                    Assert.That(Tokens(runtime), Is.Empty, "Arrange: nothing of hers is standing.");
                }

                yield return YieldUntilTheNamedSeatActs(runtime, enemy);
                yield return PointTrayAt(root, enemy);
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(her).transform.position),
                    Is.True,
                    "Shock's printed Attack 5 reaches her where she stands. " + Remaining(runtime));
                yield return null;

                Assert.That(
                    runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                        offer.SourceDefinitionReference.DefinitionId.Equals(MinCardContent.Poof)),
                    Is.True,
                    "p.14 offers her Block as a whole face whether or not its condition is met - " +
                    "8.40g's law is that the preview and the resolver agree, not that an " +
                    "unfulfillable Block is hidden. " + Remaining(runtime));
                var herSpaceBefore = Position(runtime, her);
                yield return ClickDefenseFace(root, MinCardContent.Poof);
                yield return null;
                yield return null;

                if (withABomb)
                {
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(her)), Is.True,
                        "\"you successfully defend, regardless of the attack value\". " +
                        Remaining(runtime));
                    Assert.That(Position(runtime, her), Is.EqualTo(bombAt),
                        "and p.19's Swap put her where the bomb was. " + Remaining(runtime));
                    Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(herSpaceBefore),
                        "and the bomb where she was - one physical piece exchanged, neither a " +
                        "movement nor a placement.");
                    Assert.That(HeroView(her).Coordinate, Is.EqualTo(bombAt));
                }
                else
                {
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(her)), Is.False,
                        "MI-Q8: with nothing to swap with the Block fails, and a Block card " +
                        "printing no Defense value has nothing left to compare - p.14 defeats " +
                        "her. " + Remaining(runtime));
                    Assert.That(
                        Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                            .Count(view => view.Occupancy.HeroId.Equals(her) &&
                                view.gameObject.activeSelf),
                        Is.Zero,
                        "and her piece leaves the board.");
                }

                SeatsAgree(runtime, withABomb ? "the vanishing block" : "the failed block");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Milestone 8.20c's standard: every verified face she prints, drawn at the validated player
        /// viewport, is exactly one <c>CardFootprint</c> and shows its title, values, action, rules text
        /// and history whole. Measured over all NINETEEN, the boxed Handicap included - a card nobody
        /// can play is still a card somebody can look at.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryMinFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMin);
            var root = Root();
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Min", MinCardContent.AllDefinitions, root.Hud);
        }
    }
}

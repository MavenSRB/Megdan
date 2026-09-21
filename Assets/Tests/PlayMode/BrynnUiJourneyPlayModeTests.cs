using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
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
    /// Brynn the Seeker's piece, his tray, his two basics and his three Tier I faces, driven through
    /// real pointer events and camera rays. Milestone 8.69h, and the TWENTIETH hero to take the
    /// permanent player-path gate.
    ///
    /// <para><b>THE STAGED BOARDS WERE MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, offline and at
    /// the authoritative tier, which is the lesson milestone 8.38f records and the reason every offer
    /// below is asserted as an exact set. Milestone 8.69g's engagement stands him at (-3,1) and the one
    /// enemy hero at (-2,2), with a friendly Blue melee minion at (-3,2), an enemy Red ranged minion at
    /// (-4,2), enemy Red melee minions at (-1,-1) and (-1,0), a friendly Blue heavy at (0,0), a
    /// friendly Blue ranged at (-3,4), an enemy Red melee at (-4,4), and terrain at (-3,3), (-4,3),
    /// (-3,-2), (-2,-2), (-2,-3), (1,-2), (1,-4), (2,-4) and (-6,2).</para>
    ///
    /// <para><b>EVERY STAGING OF HIS IS A COUNT OF OBSTACLES, because that is what his whole set reads.</b>
    /// Fifteen of his nineteen printed faces ask whether a hero is "adjacent to 3 or more obstacles"
    /// (BR-Q1), so a board on which nobody qualifies exercises only the negative arm of every clause he
    /// prints. The four stagings below were each measured with
    /// <c>ObstacleAdjacencyQuery</c>'s own reading, and each one is named for the number it builds:</para>
    /// <list type="bullet">
    /// <item><description><see cref="BoxedIn"/> (-2,1): the enemy hero ADJACENT to Brynn with EXACTLY
    /// THREE obstacle neighbours - Brynn himself at (-3,1), the friendly melee minion at (-3,2) and the
    /// enemy melee minion at (-1,0). The Gold's own reminder is what makes Brynn one of them.</description></item>
    /// <item><description><see cref="InTheOpen"/> (-3,0): the enemy hero adjacent to Brynn with ONE
    /// obstacle neighbour, which is Brynn. It is the negative arm of every clause, and the only thing
    /// that differs from the staging above is where the enemy stands.</description></item>
    /// <item><description><see cref="AtRange"/> (-1,1) with the enemy melee minion moved to (-1,2): the
    /// enemy hero TWO spaces away with three obstacle neighbours - the friendly heavy at (0,0) and the
    /// two enemy melee minions. It is the one staging that separates the "adjacent to you" arm of his
    /// modals from the "in radius" arm, because the adjacent arm has no hero to name at all.</description></item>
    /// <item><description><see cref="EdgeSpace"/> (1,-3) with Brynn at (-1,-2): the enemy hero boxed in
    /// by THE WATER RING ALONE - (1,-2), (1,-4) and (2,-4) are p.19 terrain and no unit stands beside
    /// him. BR-Q13, settled by the user on 2026-09-21, and
    /// <see cref="TheWaterRingAloneBoxesInAHeroStandingOnTheMapsEdge"/> is where a player sees it.</description></item>
    /// <item><description><see cref="Alone"/> (-6,0) with the enemy at (-6,3): nothing at all beside
    /// Brynn and one obstacle beside the enemy, which is the board on which Familiar Ground is GREYED
    /// because neither printed bullet names anybody.</description></item>
    /// </list>
    ///
    /// <para><b>A FINDING THIS BLOCK MEASURED AND DID NOT CHANGE, recorded where the journey that meets
    /// it lives.</b> A p.10 modal bullet whose subject is chosen by <c>attack-sequence@1</c> is offered
    /// even when its own sentence names nobody, because
    /// <c>SyntheticSequenceCapabilities.AttackSequenceHandler.Anticipate</c> deliberately answers
    /// <c>Applied</c> for any readable bullet - "whether that bullet's sequence will find a unit is
    /// p.14's question, asked when the sequence starts". Every OTHER arm of his - both trap bullets,
    /// both guide sentences, the swap's two reaches - is greyed correctly, because the capabilities
    /// behind those do derive their candidates. So on a board where a unit stands beside him and no
    /// hero in range is boxed in, Familiar Ground's second bullet is a live control that spends the
    /// action and applies nothing.
    /// <see cref="FamiliarGroundTakesEitherPrintedBulletAndIsGreyedWhenNeitherNamesAnybody"/> pins that
    /// measurement rather than asserting around it: the day the reading changes, that assertion is the
    /// one that fails first. It predates this hero - Bain's Bounty Hunter, Dodger's Middlefinger of
    /// Death and Whisper's Swift Justice all print a narrowed bullet on the same contract - so it is
    /// escalated in this milestone's report rather than fixed inside a gate.</para>
    /// </summary>
    public sealed class BrynnUiJourneyPlayModeTests
    {
        /// <summary>Where the sandbox stands him: the engagement's own space for the Blue hero.</summary>
        internal static readonly HexCoordinate HisSpace = new HexCoordinate(-3, 1);

        /// <summary>
        /// MEASURED: the enemy hero here is adjacent to Brynn AND has exactly three obstacle
        /// neighbours, so every "adjacent to you" arm and every "adjacent to 3 or more obstacles"
        /// clause of his is satisfied at once.
        /// </summary>
        internal static readonly HexCoordinate BoxedIn = new HexCoordinate(-2, 1);

        /// <summary>
        /// MEASURED: adjacent to Brynn with ONE obstacle neighbour - Brynn. The negative arm.
        /// </summary>
        internal static readonly HexCoordinate InTheOpen = new HexCoordinate(-3, 0);

        /// <summary>
        /// MEASURED with <see cref="MinionBesideTheRangedEnemy"/>: two spaces from Brynn with three
        /// obstacle neighbours, and nothing of his own ring is touched.
        /// </summary>
        internal static readonly HexCoordinate AtRange = new HexCoordinate(-1, 1);

        /// <summary>The enemy melee minion's staged space when a journey wants the hero boxed in at range.</summary>
        internal static readonly HexCoordinate MinionBesideTheRangedEnemy = new HexCoordinate(-1, 2);

        /// <summary>BR-Q13: three of this space's six neighbours are the unplayable water ring.</summary>
        internal static readonly HexCoordinate EdgeSpace = new HexCoordinate(1, -3);

        /// <summary>Two spaces from <see cref="EdgeSpace"/>, inside every Radius his set prints.</summary>
        internal static readonly HexCoordinate NearTheEdge = new HexCoordinate(-1, -2);

        /// <summary>Nothing stands beside this space, so no "adjacent to you" arm of his names anybody.</summary>
        internal static readonly HexCoordinate Alone = new HexCoordinate(-6, 0);

        /// <summary>Three spaces from <see cref="Alone"/> with one obstacle neighbour.</summary>
        internal static readonly HexCoordinate FarEnemy = new HexCoordinate(-6, 3);

        /// <summary>The enemy melee minion the ranged staging relocates.</summary>
        internal static readonly MinionId RedMelee = new MinionId("red-melee-01");

        /// <summary>The friendly melee minion the engagement leaves inside his ring.</summary>
        internal static readonly MinionId BlueMelee = new MinionId("blue-melee-01");

        /// <summary>The enemy ranged minion the engagement leaves inside his ring.</summary>
        internal static readonly MinionId RedRanged = new MinionId("red-ranged-01");

        /// <summary>The friendly ranged minion three spaces off, which is the guides' second unit.</summary>
        internal static readonly MinionId BlueRanged = new MinionId("blue-ranged-01");

        /// <summary>The staged relocation that boxes the enemy hero in at <see cref="AtRange"/>.</summary>
        internal static KeyValuePair<MinionId, HexCoordinate>[] BoxTheRangedEnemyIn() =>
            new[]
            {
                new KeyValuePair<MinionId, HexCoordinate>(RedMelee, MinionBesideTheRangedEnemy)
            };

        /// <summary>
        /// The staged seat dealt Brynn's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. A staged arm missing from
        /// <c>SelectSetupConfiguration</c> deals his cards to a seat whose content identity is somebody
        /// else's, which is milestone 8.38f's defect and what this assertion exists to catch.
        /// </summary>
        internal static HeroId Brynn(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == BrynnCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Brynn to exactly one seat; it dealt him to " +
                seats.Length + ".");
            return seats[0].HeroId;
        }

        /// <summary>Every seat that is not his.</summary>
        internal static HeroId[] Foes(ConfiguredMatchSceneRuntime runtime)
        {
            var his = Brynn(runtime);
            return runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(his))
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
        internal static IEnumerator StageBrynn(
            SceneCardContent content,
            IEnumerable<CardDefinitionId> hand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            HexCoordinate? hisSpace = null,
            HexCoordinate? enemySpace = null,
            IEnumerable<KeyValuePair<MinionId, HexCoordinate>> movedMinions = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var his = Brynn(runtime);
            var foes = Foes(runtime);

            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(his, hisSpace ?? HisSpace, hand: hand),
                new SandboxHeroStaging(foes[0], enemySpace ?? BoxedIn, hand: enemyHand)
            };
            for (var index = 1; index < foes.Length; index++)
            {
                heroes.Add(new SandboxHeroStaging(foes[index], new HexCoordinate(-5, 1)));
            }

            MatchSceneCompositionRoot.NextComposedSandboxStaging =
                new SandboxStagingDescription(heroes: heroes, movedMinions: movedMinions);
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// His own tray card, addressed by the face it prints rather than by any text that face
        /// MENTIONS. Min's 8.61k lesson: the shared <c>UiPlayer.NamedElementWithText</c> matches a tray
        /// card's assistive tooltip by SUBSTRING, and the tooltip is composed as
        /// "{DisplayName} - {ColorName} - ..." (<c>TrayCardViewModel.AssistiveLabel</c>), so a face is
        /// named by what its tooltip STARTS with - which is the card and never the sentence. No printed
        /// sentence of his names another face of his, but the prefix match costs nothing and removes the
        /// whole class of failure.
        /// </summary>
        internal static VisualElement HisTrayFace(ConfiguredMatchHudRenderer hud, string printedName)
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
        internal static IEnumerator CommitHisFaceThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            UiPlayer.EnsureVisible(HisTrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(HisTrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        /// <summary>
        /// Commits his named face and one card from every other seat, through each seat's own tray.
        /// The enemy's card is NAMED wherever a journey cares about turn ORDER, and otherwise the
        /// seat's lowest printed Initiative, which acts last.
        /// </summary>
        internal static IEnumerator CommitEverySeatOfHis(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            CardDefinitionId enemyFace = default)
        {
            var runtime = root.Runtime;
            yield return PointTrayAt(root, Brynn(runtime));
            yield return CommitHisFaceThroughTray(root, NameOf(runtime, face));
            foreach (var seat in Foes(runtime))
            {
                yield return PointTrayAt(root, seat);
                var named = enemyFace.IsDefined && HandOf(runtime, seat)
                    .Any(definition => definition.Id.Equals(enemyFace));
                yield return CommitHisFaceThroughTray(
                    root,
                    named ? NameOf(runtime, enemyFace) : LowestInitiativeCardOf(runtime, seat));
            }
        }

        /// <summary>Commits every seat, drives the turn to him and starts the printed action.</summary>
        internal static IEnumerator BeginBrynn(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default)
        {
            var runtime = root.Runtime;
            var his = Brynn(runtime);
            yield return CommitEverySeatOfHis(root, face, enemyFace);
            yield return WaitForActor(root, his);
            yield return PointTrayAt(root, his);
            yield return StartActionThroughControl(root, kind);
            Healthy(runtime);
        }

        /// <summary>
        /// Whether the named face's printed primary action is drawn as an AVAILABLE control on his own
        /// tray, which is milestone 8.35l's greying read where a player reads it. A face whose every
        /// printed clause names nobody is greyed rather than offered, drawn and spent.
        /// </summary>
        internal static bool PrimaryIsOffered(
            ConfiguredMatchSceneRuntime runtime, CardDefinitionId face) =>
            runtime.Projection.AvailableTurnActions.Any(offer =>
                offer.IsAvailable &&
                runtime.Cards.Single(card => card.Id.Equals(offer.SourceCardInstanceId))
                    .DefinitionReference.DefinitionId.Equals(face) &&
                runtime.Definitions
                    .Single(definition =>
                        definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Any(action => action.Id.Equals(offer.ActionDefinitionId) &&
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary));

        /// <summary>
        /// Every seat's own published projection agrees about the board, about whose decision is
        /// standing and about the round. Two of his printed clauses put a question to a seat that is not
        /// the acting one - both trap bullets ask the CHOSEN hero which card they discard - and the
        /// thing that could silently be wrong about them is not whether the prompt opens but whether
        /// every seat is told the same thing about it.
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
                    string.Join(",", projection.Minions
                        .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                        .Select(minion => minion.MinionId.Value + "@" + minion.Coordinate)) + " | " +
                    projection.RoundIndex + "/" + projection.TurnIndex)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(readings, Has.Length.EqualTo(1),
                "Every seat's projection must agree at " + moment + "; they read: " +
                string.Join(" || ", readings));
        }

        /// <summary>Every card of this hero's currently in the discard pile, as definition ids.</summary>
        internal static string[] DiscardsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>Every card of this hero's currently in hand, as definition ids.</summary>
        internal static string[] HeldBy(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// How many of a space's six neighbours are obstacles under rulebook p.18, read off the
        /// projection a player is looking at. Every staging of his states this number, because it is
        /// the one number his whole set turns on and a staging whose count drifted would prove the
        /// opposite arm of every clause without saying so.
        /// </summary>
        internal static int ObstaclesBeside(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate around)
        {
            var occupied = runtime.Projection.Heroes.Select(hero => hero.Coordinate)
                .Concat(runtime.Projection.Minions.Select(minion => minion.Coordinate))
                .Concat(runtime.Projection.Obstacles.Select(obstacle => obstacle.Coordinate))
                .ToArray();
            return around.GetNeighbors().Count(neighbour =>
                occupied.Any(value => value.Equals(neighbour)) ||
                !runtime.Projection.SelectedMap.Spaces.Any(space =>
                    space.Coordinate.Equals(neighbour) &&
                    space.Kind == BoardMapSpaceKind.Playable));
        }

        /// <summary>Presses one of a standing modal's printed bullets, by the node it stands for.</summary>
        internal static IEnumerator Bullet(MatchSceneCompositionRoot root, string optionNodeId)
        {
            Assert.That(OfferedBullets(root.Runtime), Contains.Item(optionNodeId),
                "'" + optionNodeId + "' is not one of the bullets this modal is offering. " +
                Remaining(root.Runtime));
            UiPlayer.ClickEffectOption(root.Runtime, root.Hud, optionNodeId);
            yield return null;
            yield return null;
            Healthy(root.Runtime);
        }

        /// <summary>
        /// Milestone 8.70. Every bullet a standing modal has GREYED, as the node ids they stand for -
        /// the set the decision window draws with a label, a place and no command behind it.
        /// </summary>
        internal static string[] GreyedBullets(ConfiguredMatchSceneRuntime runtime)
        {
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "No modal stands. " + Remaining(runtime));
            return choice.UnavailableCandidates
                .Where(candidate => candidate.OptionNodeId.IsDefined)
                .Select(candidate => candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>Every bullet a standing modal is offering, as the node ids they stand for.</summary>
        internal static string[] OfferedBullets(ConfiguredMatchSceneRuntime runtime)
        {
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "No modal stands. " + Remaining(runtime));
            return choice.SelectInvocations
                .Where(invocation => invocation.Candidate.OptionNodeId.IsDefined)
                .Select(invocation => invocation.Candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        // -------------------------------------------------------------------------------------------
        // The component half his piece owns, on his own dealt board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// His own token stands at the configured Blue spawn as Brynn and only once, a camera ray on it
        /// points the tray at him, and his tray commits a face through rulebook p.7's own reveal.
        ///
        /// <para>The other half of his identity - that the 8.35z86 picker finds him by his three stars
        /// and each of his three printed roles, draws his own pick card on hover and hides him for a
        /// role he does not carry - is
        /// <c>PrivateLobbyPlayModeTests.BrynnIsFoundByHisThreeStarsAndEachOfHisThreeRoles</c>, which his
        /// gate's own piece row names beside this one. It is the user's one named requirement for the
        /// twentieth hero and it is proved on the real selection screen rather than here, because that
        /// screen is a different scene.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisPieceSpawnsAsBrynnAndHisTrayCommitsAndRevealsACard()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedEngagedStagedBrynn,
                hand: new[]
                {
                    BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy,
                    BrynnCardContent.HighGround, BrynnCardContent.BearTrap
                });
            var root = Root();
            var runtime = root.Runtime;
            var his = Brynn(runtime);

            var pieces = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                .Where(view => view.Occupancy.HeroId.Equals(his))
                .ToArray();
            Assert.That(pieces, Has.Length.EqualTo(1),
                "One hero, one piece - a second would be a view the rebuild failed to retire.");
            Assert.That(pieces[0].Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(his)).Coordinate),
                "and it stands where the authority says he does.");
            Assert.That(
                runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(his)).SpawnPoint.ToString(),
                Does.Contain("blue-hero-01"),
                "The staged engagement preserves his configured spawn point, which is what p.13's " +
                "respawn later returns him to.");

            yield return PointTrayAt(root, his);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(his),
                "A camera ray on his own piece points the tray at him.");

            yield return CommitHisFaceThroughTray(
                root, NameOf(runtime, BrynnCardContent.FamiliarGround));
            Assert.That(runtime.GetCommitmentStatus(his),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown).Or
                    .EqualTo(TurnCardCommitmentStatus.Revealed),
                "CONFIRM FACEDOWN puts his card in p.7's turn area. " + Remaining(runtime));
            foreach (var seat in Foes(runtime))
            {
                yield return PointTrayAt(root, seat);
                yield return CommitHisFaceThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }

            Assert.That(runtime.GetCommitmentStatus(his),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed),
                "and the reveal follows the last seat's commitment. " + Remaining(runtime));
            SeatsAgree(runtime, "the reveal");
            Healthy(runtime);
        }

        /// <summary>
        /// Every face he prints that carries rulebook p.10's secondary Movement ribbon walks the board
        /// through its own card face, and every face he prints that carries p.14's secondary Defense
        /// shield is offered in the defender's own prompt.
        ///
        /// <para>BR-06 splits his set exactly once: <b>Decoy prints a Defense 2 and NO Movement ribbon
        /// at all</b>, so the walk half skips it and the defend half does not. Every one of his other
        /// seventeen dealt faces prints both, and Over the Top prints no action whatsoever and cannot be
        /// committed, walked or discarded to defend.</para>
        ///
        /// <para>The loop runs on the Tier I scene for every face, including his Tier II and Tier III
        /// ones: a staged hand may take a card out of its owner's own DECK
        /// (<c>SandboxStagingAuthority.StageHands</c>), so the secondary of a face he has not levelled
        /// into is still his own printed secondary on his own card.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in BrynnCardContent.DealtDefinitions
                .Where(card => card.ActionDefinitions.Count > 0))
            {
                var walks = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary);
                var defends = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction);
                Assert.That(defends, Is.True,
                    definition.DisplayName + " prints p.14's secondary Defense shield, which every " +
                    "dealt face of his does.");

                if (walks)
                {
                    yield return StageBrynn(
                        SceneCardContent.VerifiedEngagedStagedBrynn,
                        hand: new[] { definition.Id, BrynnCardContent.Decoy });
                    var root = Root();
                    var runtime = root.Runtime;
                    var his = Brynn(runtime);
                    yield return BeginBrynn(root, definition.Id, ActionKind.Movement);
                    Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                        definition.DisplayName + "'s printed boot opens a walk. " +
                        Remaining(runtime));
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
                    Assert.That(Position(runtime, his), Is.EqualTo(step),
                        "and his piece follows the authoritative space. " + Remaining(runtime));
                    Assert.That(HeroView(his).Coordinate, Is.EqualTo(step));
                    Healthy(runtime);
                }

                // He commits a card that is NOT the one under test and that acts after the enemy's
                // Shock (printed Initiative 8), so the attack he answers is the enemy's own turn.
                var committed = definition.Id.Equals(BrynnCardContent.Decoy)
                    ? BrynnCardContent.BearTrap
                    : BrynnCardContent.Decoy;
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { definition.Id, committed },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var defenseRoot = Root();
                var defenseRuntime = defenseRoot.Runtime;
                var defender = Brynn(defenseRuntime);
                var enemy = Foes(defenseRuntime)[0];
                yield return PointTrayAt(defenseRoot, defender);
                yield return CommitHisFaceThroughTray(
                    defenseRoot, NameOf(defenseRuntime, committed));
                yield return PointTrayAt(defenseRoot, enemy);
                yield return CommitHisFaceThroughTray(
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
                    "and answering with it spends it into his own discard pile.");
                Healthy(defenseRuntime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // His Gold: the modal Attack
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Familiar Ground: "Choose one - (1) Target a unit adjacent to you. (2) Target a hero in range
        /// who is adjacent to 3 or more obstacles."
        ///
        /// <para><b>Three boards, each measured, each proving a different thing.</b> On
        /// <see cref="BoxedIn"/> both printed bullets name somebody and each is taken in its own run:
        /// the adjacent bullet reaches the enemy hero AND the enemy ranged minion beside him, and the
        /// obstacle bullet reaches the ONE hero the narrowing admits and no minion at all - which is
        /// milestone 8.69a's <c>AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles</c> read
        /// at p.14's target selection. On <see cref="Alone"/> neither bullet names anybody and the whole
        /// printed primary is GREYED on his own tray, which is milestone 8.35l's rule. On
        /// <see cref="InTheOpen"/> a unit stands beside him and no hero in range is boxed in, and the
        /// second bullet is a live control that applies nothing - the finding this fixture's own remarks
        /// name, pinned here rather than asserted around.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FamiliarGroundTakesEitherPrintedBulletAndIsGreyedWhenNeitherNamesAnybody()
        {
            foreach (var obstacleBullet in new[] { false, true })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, BoxedIn), Is.EqualTo(3),
                    "Arrange: EXACTLY the printed threshold beside the enemy hero, so a reading " +
                    "that passed on four would still be measuring three. " + Remaining(runtime));

                yield return BeginBrynn(
                    root, BrynnCardContent.FamiliarGround, ActionKind.Attack, WaspCardContent.Shock);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(new[]
                    {
                        "familiar-ground-adjacent-bullet", "familiar-ground-obstacle-bullet"
                    }).AsCollection,
                    "p.10's \"Choose one -\" is drawn as its two printed bullets. " +
                    Remaining(runtime));
                yield return Bullet(
                    root,
                    obstacleBullet
                        ? "familiar-ground-obstacle-bullet"
                        : "familiar-ground-adjacent-bullet");

                var target = runtime.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null,
                    "Taking a bullet reaches p.14's own target question. " + Remaining(runtime));
                Assert.That(target.LegalTargetHeroIds.ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection);
                if (obstacleBullet)
                {
                    Assert.That(target.LegalTargetMinionIds, Is.Empty,
                        "\"Target a HERO in range who is adjacent to 3 or more obstacles\" - the " +
                        "enemy minion standing beside him is not a hero and is not offered. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(target.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                        Is.EqualTo(new[] { RedRanged.Value }).AsCollection,
                        "\"Target a UNIT adjacent to you\" - the one enemy minion the engagement " +
                        "leaves in his ring. " + Remaining(runtime));
                }

                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True,
                    "and the target is answered on the board. " + Remaining(runtime));
                yield return null;
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                SeatsAgree(runtime, "Familiar Ground's " + (obstacleBullet ? "second" : "first") +
                    " bullet");
                Healthy(runtime);
            }

            // Neither bullet names anybody: nothing at all stands beside him and the one enemy hero
            // in range has a single obstacle neighbour.
            yield return StageBrynn(
                SceneCardContent.VerifiedEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy },
                hisSpace: Alone,
                enemySpace: FarEnemy);
            var greyRoot = Root();
            var greyRuntime = greyRoot.Runtime;
            Assert.That(ObstaclesBeside(greyRuntime, FarEnemy), Is.LessThan(3),
                "Arrange: the enemy hero is in the open. " + Remaining(greyRuntime));
            Assert.That(ObstaclesBeside(greyRuntime, Alone), Is.Zero,
                "and nothing at all stands in p.19's ring around Brynn. " + Remaining(greyRuntime));
            yield return CommitEverySeatOfHis(greyRoot, BrynnCardContent.FamiliarGround);
            yield return WaitForActor(greyRoot, Brynn(greyRuntime));
            yield return PointTrayAt(greyRoot, Brynn(greyRuntime));
            Assert.That(PrimaryIsOffered(greyRuntime, BrynnCardContent.FamiliarGround), Is.False,
                "Both printed bullets name nobody, so milestone 8.35l greys the whole printed " +
                "primary rather than offering it, drawing it and spending it. " +
                Remaining(greyRuntime));
            Healthy(greyRuntime);

            // MEASURED at 8.69h as a FINDING - the narrowed bullet was offered although its own
            // sentence named nobody, and taking it spent the action and applied nothing - and FLIPPED
            // at milestone 8.70, which is the user settling BR-Q19h on that finding: "I think we
            // should gray it out if no legal targets". A unit stands beside him, so the first bullet
            // has a subject and the primary is offered; the second names nobody and is now greyed at
            // the choice, on the rendered tier as well as in the authority.
            yield return StageBrynn(
                SceneCardContent.VerifiedEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy },
                enemySpace: InTheOpen);
            var emptyRoot = Root();
            var emptyRuntime = emptyRoot.Runtime;
            Assert.That(ObstaclesBeside(emptyRuntime, InTheOpen), Is.LessThan(3),
                "Arrange: the enemy hero stands in the open. " + Remaining(emptyRuntime));
            yield return BeginBrynn(
                emptyRoot, BrynnCardContent.FamiliarGround, ActionKind.Attack);
            Assert.That(GreyedBullets(emptyRuntime),
                Is.EqualTo(new[] { "familiar-ground-obstacle-bullet" }).AsCollection,
                "The narrowed bullet names nobody on this board, so it is greyed rather than offered, " +
                "taken and spent on nothing. " + Remaining(emptyRuntime));
            Assert.That(OfferedBullets(emptyRuntime),
                Is.EqualTo(new[] { "familiar-ground-adjacent-bullet" }).AsCollection,
                "and the bullet whose sentence does name a unit is the answer left to give. " +
                Remaining(emptyRuntime));
            Assert.That(
                emptyRuntime.RoundView.Model.UnavailableEffectChoices, Has.Count.EqualTo(1),
                "THE RENDERED TIER AGREES WITH THE AUTHORITY, which is the half a headless case " +
                "cannot see: the window draws the greyed bullet with its label and its place and no " +
                "command behind it. " + Remaining(emptyRuntime));
            yield return Bullet(emptyRoot, "familiar-ground-adjacent-bullet");
            Assert.That(emptyRuntime.PendingAttackTargetChoice, Is.Not.Null,
                "and the answer the board supports reaches p.14's own target question. " +
                Remaining(emptyRuntime));
            SeatsAgree(emptyRuntime, "the greyed bullet");
            Healthy(emptyRuntime);
        }

        /// <summary>
        /// BR-Q13, settled by the user on 2026-09-21: rulebook p.19 makes the water spaces surrounding
        /// the board terrain and therefore obstacles, so a hero standing on the map's edge is boxed in
        /// by the edge alone.
        ///
        /// <para>MEASURED: (1,-3) has exactly three unplayable neighbours - (1,-2), (1,-4) and (2,-4) -
        /// and NO unit stands beside it, so the only thing making the enemy hero a legal subject of
        /// Familiar Ground's second bullet is the water. Brynn stands two spaces off at (-1,-2), inside
        /// the printed Range 3 and outside p.19's ring, so the first bullet cannot be what offers
        /// him.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheWaterRingAloneBoxesInAHeroStandingOnTheMapsEdge()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                hisSpace: NearTheEdge,
                enemySpace: EdgeSpace);
            var root = Root();
            var runtime = root.Runtime;
            var enemy = Foes(runtime)[0];

            Assert.That(ObstaclesBeside(runtime, EdgeSpace), Is.EqualTo(3),
                "Arrange: exactly three obstacles beside him. " + Remaining(runtime));
            Assert.That(
                EdgeSpace.GetNeighbors().Count(neighbour =>
                    runtime.Projection.Heroes.Any(hero => hero.Coordinate.Equals(neighbour)) ||
                    runtime.Projection.Minions.Any(minion => minion.Coordinate.Equals(neighbour))),
                Is.Zero,
                "and not one of them is a unit - the water ring is the whole count, which is what " +
                "BR-Q13 says p.19 means. " + Remaining(runtime));
            Assert.That(Distance(NearTheEdge, EdgeSpace), Is.EqualTo(2),
                "and Brynn is two away, so the bullet that offers him cannot be the adjacent one.");

            yield return BeginBrynn(
                root, BrynnCardContent.FamiliarGround, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(OfferedBullets(runtime),
                Contains.Item("familiar-ground-obstacle-bullet"), Remaining(runtime));
            Assert.That(GreyedBullets(runtime),
                Does.Not.Contain("familiar-ground-obstacle-bullet"),
                "Milestone 8.70: the water ring is what gives this bullet a subject, so the bullet " +
                "the greying reads the board for is offered rather than greyed - the same phrase, " +
                "asked by the same owner, at the choice instead of at the target question. " +
                Remaining(runtime));
            yield return Bullet(root, "familiar-ground-obstacle-bullet");
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "The hero the water ring boxes in is the one hero the narrowing admits. " +
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
            SeatsAgree(runtime, "the water ring's own hero");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // His Red Tier I: the printed plus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// High Ground: "Target a unit adjacent to you. If you target a hero adjacent to 3 or more
        /// obstacles, +2 attack."
        ///
        /// <para><b>The +2 is read where a player reads it</b> - in the Attack total on the decision
        /// window's own Defense breakdown, which is the row rulebook p.14 computes at step 4. The same
        /// printed Attack 4 against the same hero on two measured boards reads 6 and 4, and the boxed-in
        /// run carries a contribution row for the bonus that the open run does not.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HighGroundAddsItsPrintedTwoBesideThreeObstaclesAndNothingInTheOpen()
        {
            foreach (var boxedIn in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.HighGround, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: boxedIn ? BoxedIn : InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];
                Assert.That(
                    ObstaclesBeside(runtime, boxedIn ? BoxedIn : InTheOpen),
                    boxedIn ? Is.EqualTo(3) : Is.LessThan(3),
                    "Arrange: the one thing that differs between the two runs. " +
                    Remaining(runtime));

                yield return BeginBrynn(
                    root, BrynnCardContent.HighGround, ActionKind.Attack, WaspCardContent.Shock);
                Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "\"a unit adjacent to you\" - the one enemy hero beside him. " +
                    Remaining(runtime));
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;

                var breakdown = runtime.RoundView.Model.DefenseChoices
                    .Select(choice => choice.DefenseBreakdown)
                    .FirstOrDefault(value => value != null);
                Assert.That(breakdown, Is.Not.Null,
                    "p.14's Defense question draws the Attack total the defender is answering. " +
                    Remaining(runtime));
                Assert.That(breakdown.AttackTotal, Is.EqualTo(boxedIn ? 6 : 4),
                    "His printed Attack 4, plus the printed 2 exactly when the target is boxed in. " +
                    "Rows: " + string.Join(", ", breakdown.AttackRows.Select(row =>
                        row.SourceLabel + " " + row.SignedAmount)) + ". " + Remaining(runtime));
                Assert.That(
                    breakdown.AttackRows.Count(row => row.Amount == 2),
                    Is.EqualTo(boxedIn ? 1 : 0),
                    "and the bonus is its own named row rather than a bigger printed number. " +
                    Remaining(runtime));

                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                SeatsAgree(runtime, boxedIn ? "the printed plus" : "the plain attack");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // His Silver: Decoy
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Decoy: "Choose up to two times, on different targets - (1) Move an enemy minion in radius 1
        /// space. (2) Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space."
        ///
        /// <para><b>BR-Q4's three reachable outcomes, and the journey drives all three.</b> The first
        /// performance takes the HERO bullet - which is offered only because the enemy hero is boxed in -
        /// and moves him one space of Brynn's choosing; the second is asked separately, no longer offers
        /// a hero at all because the one hero the narrowing admits has already been named, and takes a
        /// minion instead. A second run declines the first "may" outright and nobody moves, which is the
        /// zero-performance outcome BR-Q4 states.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DecoyMovesTwoEnemyUnitsOneSpaceEachOrMovesNobodyAtAll()
        {
            foreach (var accept in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.Decoy, BrynnCardContent.HighGround });
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, BoxedIn), Is.EqualTo(3), Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.Decoy, ActionKind.Skill);
                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null, Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("decoy-1-may"));
                Assert.That(may.DeclineInvocation, Is.Not.Null,
                    "\"Choose UP TO two times\" - nought is one of the answers. " +
                    Remaining(runtime));

                var enemyWas = Position(runtime, enemy);
                if (!accept)
                {
                    yield return May(root, accept: false);
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "Declining the first performance ends the printed sentence. " +
                        Remaining(runtime));
                    Assert.That(Position(runtime, enemy), Is.EqualTo(enemyWas),
                        "and nobody moved. " + Remaining(runtime));
                    SeatsAgree(runtime, "Decoy declined");
                    Healthy(runtime);
                    continue;
                }

                yield return May(root, accept: true);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(new[] { "decoy-1-hero", "decoy-1-minion" }).AsCollection,
                    "Both printed bullets name somebody on this board. " + Remaining(runtime));
                yield return Bullet(root, "decoy-1-hero");

                var heroTarget = runtime.PendingEffectChoice;
                Assert.That(heroTarget.NodeId.Value, Is.EqualTo("decoy-1-hero-target"));
                Assert.That(
                    heroTarget.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "\"an enemy hero in radius who is adjacent to 3 or more obstacles\". " +
                    Remaining(runtime));
                yield return Pick(root, heroTarget.Candidates.Single());

                var heroMove = runtime.PendingEffectChoice;
                Assert.That(heroMove.NodeId.Value, Is.EqualTo("decoy-1-hero-move"));
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, enemyWas) == 1),
                    Is.True,
                    "\"1 space\" is an exact distance, and Brynn picks which one (p.10). " +
                    Remaining(runtime));
                var destination = OfferedSpaces(runtime).First();
                yield return Space(root, destination);
                Assert.That(Position(runtime, enemy), Is.EqualTo(destination),
                    "and the enemy hero stands where Brynn put him. " + Remaining(runtime));
                Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(destination));

                var second = runtime.PendingEffectChoice;
                Assert.That(second, Is.Not.Null,
                    "\"up to TWO times\" asks a second time. " + Remaining(runtime));
                Assert.That(second.NodeId.Value, Is.EqualTo("decoy-2-after-hero-may"));
                yield return May(root, accept: true);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(new[] { "decoy-2-after-hero-minion" }).AsCollection,
                    "\"on DIFFERENT targets\" - the one hero the narrowing admits was named by the " +
                    "first performance, so the second offers the minion bullet alone. " +
                    Remaining(runtime));
                yield return Bullet(root, "decoy-2-after-hero-minion");

                var minionTarget = runtime.PendingEffectChoice;
                Assert.That(minionTarget.NodeId.Value,
                    Is.EqualTo("decoy-2-after-hero-minion-target"));
                Assert.That(
                    minionTarget.Candidates.All(candidate =>
                        candidate.Value.Kind == EffectValueKind.Minion),
                    Is.True,
                    "and every candidate is an enemy minion in his printed Radius 4. " +
                    Remaining(runtime));
                var minion = minionTarget.Candidates.First(candidate =>
                    candidate.Value.MinionId.Equals(RedRanged));
                var minionWas = MinionView(RedRanged).Occupancy.Coordinate;
                yield return Pick(root, minion);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("decoy-2-after-hero-minion-move"));
                var minionTo = OfferedSpaces(runtime).First();
                yield return Space(root, minionTo);
                Assert.That(MinionView(RedRanged).Occupancy.Coordinate, Is.EqualTo(minionTo));
                Assert.That(Distance(minionWas, minionTo), Is.EqualTo(1),
                    "One space, which is what the bullet prints. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "and two performances is the printed ceiling. " + Remaining(runtime));
                SeatsAgree(runtime, "Decoy's two performances");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // His Blue Tier I: the tracking swap
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Tread Lightly: "Swap either with a unit adjacent to you, or with an enemy hero in radius who
        /// is adjacent to 3 or more obstacles."
        ///
        /// <para><b>BR-Q10's one selection over two reaches, driven on both.</b> On
        /// <see cref="BoxedIn"/> the selection holds three pieces - the enemy hero, the friendly melee
        /// minion and the enemy ranged minion - and the run takes a MINION, which only the adjacent
        /// reach can offer. On <see cref="AtRange"/> the enemy hero stands two spaces off with three
        /// obstacle neighbours, outside p.19's ring entirely, and the run takes HIM, which only the
        /// radius reach can offer at his printed Radius 2. p.19's swap is neither movement nor
        /// placement, so both pieces end on each other's spaces.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TreadLightlySwapsWithAnAdjacentUnitOrWithABoxedInHeroInRadius()
        {
            foreach (var radiusArm in new[] { false, true })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.TreadLightly, BrynnCardContent.Decoy },
                    enemySpace: radiusArm ? AtRange : BoxedIn,
                    movedMinions: radiusArm ? BoxTheRangedEnemyIn() : null);
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(
                    ObstaclesBeside(runtime, radiusArm ? AtRange : BoxedIn), Is.EqualTo(3),
                    "Arrange: the enemy hero is boxed in on both boards. " + Remaining(runtime));
                Assert.That(
                    Distance(Position(runtime, his), Position(runtime, enemy)),
                    Is.EqualTo(radiusArm ? 2 : 1),
                    "and only the second board puts him outside p.19's ring. " + Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.TreadLightly, ActionKind.Skill);
                var subject = runtime.PendingEffectChoice;
                Assert.That(subject, Is.Not.Null, Remaining(runtime));
                Assert.That(subject.NodeId.Value, Is.EqualTo("tracking-swap-subject"));
                Assert.That(
                    subject.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero &&
                        candidate.Value.HeroId.Equals(enemy)),
                    Is.True,
                    "The boxed-in enemy hero is a candidate on both boards - BR-Q19d de-duplicates " +
                    "a hero both reaches admit. " + Remaining(runtime));
                Assert.That(
                    subject.Candidates
                        .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                        .Select(candidate => candidate.Value.MinionId.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray(),
                    Is.EqualTo(new[] { BlueMelee.Value, RedRanged.Value }).AsCollection,
                    "and the adjacent reach offers a unit of EITHER side, friendly or enemy. " +
                    Remaining(runtime));

                var hisSpaceBefore = Position(runtime, his);
                if (radiusArm)
                {
                    var enemySpaceBefore = Position(runtime, enemy);
                    yield return Pick(
                        root,
                        subject.Candidates.Single(candidate =>
                            candidate.Value.Kind == EffectValueKind.Hero));
                    Assert.That(Position(runtime, his), Is.EqualTo(enemySpaceBefore),
                        "p.19's Swap exchanges the two positions. " + Remaining(runtime));
                    Assert.That(Position(runtime, enemy), Is.EqualTo(hisSpaceBefore));
                    Assert.That(HeroView(his).Coordinate, Is.EqualTo(enemySpaceBefore));
                    Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(hisSpaceBefore));
                }
                else
                {
                    var minionSpaceBefore = MinionView(BlueMelee).Occupancy.Coordinate;
                    yield return Pick(
                        root,
                        subject.Candidates.Single(candidate =>
                            candidate.Value.Kind == EffectValueKind.Minion &&
                            candidate.Value.MinionId.Equals(BlueMelee)));
                    Assert.That(Position(runtime, his), Is.EqualTo(minionSpaceBefore),
                        "and a FRIENDLY minion is as legal a subject as an enemy one. " +
                        Remaining(runtime));
                    Assert.That(MinionView(BlueMelee).Occupancy.Coordinate,
                        Is.EqualTo(hisSpaceBefore));
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "Tread Lightly prints no trailing move. " + Remaining(runtime));
                SeatsAgree(runtime, radiusArm ? "the radius swap" : "the adjacent swap");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // His Green Tier I: the trap
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Bear Trap: "Choose one - (1) An enemy hero adjacent to you discards a card, if able. (2) An
        /// enemy hero in radius who is adjacent to 3 or more obstacles discards a card, if able."
        ///
        /// <para><b>Both printed bullets, and the greying between them.</b> On <see cref="BoxedIn"/>
        /// both bullets name the one enemy hero and the run takes the FIRST; on <see cref="AtRange"/>
        /// the same hero stands two spaces away, so the adjacent bullet names nobody and p.10's modal
        /// offers the radius bullet ALONE - which is the greying
        /// <c>choose-target@2</c> and <c>synthetic-radius-hero-target@1</c> derive and
        /// <c>attack-sequence@1</c> does not.</para>
        ///
        /// <para><b>The discard is the CHOSEN hero's own decision, on that hero's own seat</b>
        /// (BR-Q11), and it is answered there - the ledger's reading, and the half a journey driven
        /// from Brynn's seat alone could not tell from a working one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BearTrapTakesACardFromTheHeroBesideHimOrFromTheOneBoxedInAtRange()
        {
            foreach (var radiusArm in new[] { false, true })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.BearTrap, BrynnCardContent.Decoy },
                    enemySpace: radiusArm ? AtRange : BoxedIn,
                    movedMinions: radiusArm ? BoxTheRangedEnemyIn() : null);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];
                Assert.That(
                    ObstaclesBeside(runtime, radiusArm ? AtRange : BoxedIn), Is.EqualTo(3),
                    Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.BearTrap, ActionKind.Skill);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(radiusArm
                        ? new[] { "trap-radius" }
                        : new[] { "trap-adjacent", "trap-radius" }).AsCollection,
                    radiusArm
                        ? "With the hero two spaces off, the adjacent bullet names nobody and p.10's " +
                          "modal offers the radius bullet alone. "
                        : "Both printed bullets name the one enemy hero. " + Remaining(runtime));
                yield return Bullet(root, radiusArm ? "trap-radius" : "trap-adjacent");

                var select = runtime.PendingEffectChoice;
                Assert.That(select.NodeId.Value,
                    Is.EqualTo(radiusArm ? "trap-radius-select" : "trap-adjacent-select"));
                Assert.That(
                    select.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "and the one enemy hero the bullet reaches is the whole candidate set. " +
                    Remaining(runtime));
                var heldBefore = HandCountOf(runtime, enemy);
                yield return Pick(root, select.Candidates.Single());

                var discard = runtime.PendingEffectChoice;
                Assert.That(discard, Is.Not.Null,
                    "\"discards a card\" stops the page. " + Remaining(runtime));
                Assert.That(discard.NodeId.Value,
                    Is.EqualTo(radiusArm ? "trap-radius-discard" : "trap-adjacent-discard"));
                Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                    "BR-Q11: the CHOSEN hero picks the card they lose, on their own seat - Brynn " +
                    "set the trap and does not choose what it costs. " + Remaining(runtime));
                Assert.That(
                    discard.Candidates.All(candidate => runtime.Cards
                        .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                        .OwnerId.Equals(enemy)),
                    Is.True,
                    "and the faces offered are that hero's own hand. " + Remaining(runtime));
                SeatsAgree(runtime, "the trap's own discard");

                var wrongChooser = DirectorCommand.AnswerEffectChoice(
                    new EffectChoiceCommand(
                        discard.ResolutionId, discard.ChoiceId, EffectChoiceCommandKind.Select,
                        discard.Candidates[0], Brynn(runtime)));
                Assert.That(runtime.SubmitProjectedCommand(wrongChooser), Is.False,
                    "Brynn is refused the answer outright, which is the half a journey driven from " +
                    "his seat alone would never see. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "and the refusal leaves the question exactly where it was.");

                yield return Pick(root, discard.Candidates.First());
                Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(heldBefore - 1),
                    "One card, out of that hero's own hand. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                SeatsAgree(runtime, "the trap answered");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Milestone 8.20c's standard: every verified face he prints, drawn at the validated player
        /// viewport, is exactly one <c>CardFootprint</c> and shows its title, values, action, rules text
        /// and history whole. Measured over all NINETEEN, the boxed Handicap included - a card nobody can
        /// play is still a card somebody can look at.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBrynnFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrynn);
            var root = Root();
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Brynn", BrynnCardContent.AllDefinitions, root.Hud);
        }
    }
}

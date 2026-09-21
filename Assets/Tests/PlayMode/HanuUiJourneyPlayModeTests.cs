using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
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
    /// Hanu the Trickster's piece, his tray, his two basics and his three Tier I faces, driven through
    /// real pointer events and camera rays. Milestone 8.71i, and the TWENTY-FIRST hero to take the
    /// permanent player-path gate.
    ///
    /// <para><b>THE STAGED BOARDS WERE MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, offline and at the
    /// authoritative tier, which is the lesson milestone 8.38f records and the reason the offers below
    /// are asserted as sets rather than sampled. Milestone 8.71h's engagement stands him at (-3,1) and
    /// the one enemy hero at (-2,2) - separation two - with a friendly Blue melee minion at (-3,2), an
    /// enemy Red ranged minion at (-4,2), enemy Red melee minions at (-1,-1) and (-1,0), a friendly Blue
    /// heavy at (0,0), a friendly Blue ranged at (-3,4), and terrain at (-3,3), (-4,3), (-3,-2), (-2,-2),
    /// (-2,-3), (1,-2), (1,-4), (2,-4) and (-6,2). His free ring-1 spaces are (-2,1), (-4,1), (-3,0) and
    /// (-2,0), and exactly two straight files of three free spaces leave his own square, through (-2,1)
    /// and through (-4,1) - which is what Fight and Flight's "move 3 spaces in a straight line" needs.</para>
    ///
    /// <para><b>THE ALLY BOARD IS NOT A CONVENIENCE.</b> Four of his faces read "a friendly hero": the
    /// two co-moves ask one to choose a distance on their own seat (8.71e composed the selection with
    /// <c>includeSelf</c> false), and the SECOND BULLET of all four graded Reds narrows its candidate to
    /// a unit standing beside one - <c>AttackSequenceBulletSubjectQuery.StandsBesideAFriendlyHeroOfTheActor</c>
    /// reads <c>RosterUnitKind.Hero</c>, so a friendly MINION beside the candidate does not satisfy it.
    /// Every journey that drives the Reds' second bullet therefore stands on
    /// <c>VerifiedStagedHanuWithAlly</c>, where Arien holds the second Blue seat; that board is not
    /// restaged by its configuration, so <see cref="StageHanu"/> stands all three where the printed
    /// sentences can reach each other.</para>
    ///
    /// <para><b>INITIATIVE ORDER IS PART OF EVERY STAGING OF HIS</b>, because Hurry up! rewrites one.
    /// A HIGHER printed Initiative acts FIRST: his Silver prints 12, his Greens print 1 and 2, and the
    /// enemy's Shock prints 8 - so committing Hurry up! against Shock is what puts the rewrite on the
    /// board while the rewritten card is still unresolved, which is the only state p.10 lets it be
    /// written in.</para>
    /// </summary>
    public sealed class HanuUiJourneyPlayModeTests
    {
        /// <summary>Where the sandbox stands him: the engagement's own space for the Blue hero.</summary>
        internal static readonly HexCoordinate HisSpace = new HexCoordinate(-3, 1);

        /// <summary>The engagement's own enemy space - MEASURED two away, inside every Radius and Range of his.</summary>
        internal static readonly HexCoordinate EnemySpace = new HexCoordinate(-2, 2);

        /// <summary>MEASURED adjacent to <see cref="HisSpace"/> and to the friendly melee minion at (-3,2).</summary>
        internal static readonly HexCoordinate AdjacentEnemy = new HexCoordinate(-2, 1);

        /// <summary>MEASURED free, two spaces from him - inside Radius 2/3 and inside Range 3/4.</summary>
        internal static readonly HexCoordinate AllySpace = new HexCoordinate(-1, 1);

        /// <summary>
        /// MEASURED adjacent to <see cref="AllySpace"/> and THREE from <see cref="HisSpace"/>: inside
        /// the Range 3 his Tier I Red prints, and outside p.19's ring, which is what Trusted Sidekick's
        /// "and not adjacent to you" needs of a candidate the other bullet must not also name.
        /// </summary>
        internal static readonly HexCoordinate EnemyBesideTheAlly = new HexCoordinate(-1, 2);

        /// <summary>The friendly melee minion the engaged staging leaves inside his ring.</summary>
        internal static readonly MinionId BlueMelee = new MinionId("blue-melee-01");

        /// <summary>The enemy ranged minion the engaged staging leaves inside his ring.</summary>
        internal static readonly MinionId RedRanged = new MinionId("red-ranged-01");

        /// <summary>The friendly heavy at (0,0) - p.18 makes it immune to being swapped or placed.</summary>
        internal static readonly MinionId BlueHeavy = new MinionId("blue-heavy-01");

        /// <summary>
        /// The friendly ranged minion at (-3,4), and the second member of every friendly PAIR his
        /// Greens swap. It is this one rather than <see cref="BlueHeavy"/> for a reason milestone
        /// 8.71i measured on its first Unity run: rulebook p.18 makes a heavy minion immune to being
        /// swapped or placed, so a heavy staged beside him is not a candidate at all.
        /// </summary>
        internal static readonly MinionId BlueRanged = new MinionId("blue-ranged-01");

        /// <summary>
        /// The staged seat dealt Hanu's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. A staged arm missing from
        /// <c>SelectSetupConfiguration</c> deals his cards to a seat whose content identity is somebody
        /// else's, which is milestone 8.38f's defect and what this assertion exists to catch.
        /// </summary>
        internal static HeroId Hanu(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == HanuCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Hanu to exactly one seat; it dealt him to " +
                seats.Length + ".");
            return seats[0].HeroId;
        }

        /// <summary>His ally seat, where a configuration deals one - Arien, on the ally boards.</summary>
        internal static HeroId Ally(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == ArienCardContent.ContentSetId)
                .Select(hero => hero.HeroId)
                .SingleOrDefault();

        /// <summary>Every enemy seat, nearest first.</summary>
        internal static HeroId[] Enemies(ConfiguredMatchSceneRuntime runtime)
        {
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            return runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(his) && !hero.HeroId.Equals(ally))
                .Select(hero => hero.HeroId)
                .ToArray();
        }

        /// <summary>The match seat that owns this hero, for the seat-scoped readings a rewind needs.</summary>
        internal static SeatId SeatOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            var seat = runtime.Host.Seats.Seats.SingleOrDefault(value => value.Owns(heroId));
            Assert.That(seat, Is.Not.Null, "No seat owns " + heroId.Value + ".");
            return seat.Id;
        }

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the pieces and dealing the hands this journey needs.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the ids
        /// only exist once a configuration has been composed - the shape milestone 8.37a's Guardian
        /// Spirit journey minted and every hero since has reused.</para>
        /// </summary>
        internal static IEnumerator StageHanu(
            SceneCardContent content,
            IEnumerable<CardDefinitionId> hand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            IEnumerable<CardDefinitionId> allyHand = null,
            HexCoordinate? hisSpace = null,
            HexCoordinate? enemySpace = null,
            HexCoordinate? allySpace = null,
            IEnumerable<KeyValuePair<MinionId, HexCoordinate>> movedMinions = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var enemies = Enemies(runtime);

            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(his, hisSpace ?? HisSpace, hand: hand),
                new SandboxHeroStaging(enemies[0], enemySpace ?? EnemySpace, hand: enemyHand)
            };
            if (ally.IsDefined)
            {
                heroes.Add(new SandboxHeroStaging(ally, allySpace ?? AllySpace, hand: allyHand));
            }

            for (var index = 1; index < enemies.Length; index++)
            {
                heroes.Add(new SandboxHeroStaging(enemies[index], new HexCoordinate(-5, 1)));
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
        /// named by what its tooltip STARTS with - which is the card and never the sentence. HIS SET
        /// NEEDS IT: "Monkey Trick", "Monkey Twist" and "Monkey Business" share a first word, "This
        /// Way!" and "That Way!" differ in one letter, and The Ultimate Trick's own rules text quotes
        /// "Hurry Up!".
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
        /// Another seat's card is NAMED wherever a journey cares about turn ORDER - which for this hero
        /// is most of them, because Hurry up! rewrites one - and is otherwise that seat's lowest printed
        /// Initiative, which acts last.
        /// </summary>
        internal static IEnumerator CommitEverySeatOfHis(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            CardDefinitionId enemyFace = default,
            CardDefinitionId allyFace = default)
        {
            var runtime = root.Runtime;
            yield return PointTrayAt(root, Hanu(runtime));
            yield return CommitHisFaceThroughTray(root, NameOf(runtime, face));
            foreach (var seat in runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(Hanu(runtime)))
                .Select(hero => hero.HeroId)
                .ToArray())
            {
                yield return PointTrayAt(root, seat);
                var wanted = seat.Equals(Ally(runtime)) ? allyFace : enemyFace;
                var named = wanted.IsDefined && HandOf(runtime, seat)
                    .Any(definition => definition.Id.Equals(wanted));
                yield return CommitHisFaceThroughTray(
                    root,
                    named ? NameOf(runtime, wanted) : LowestInitiativeCardOf(runtime, seat));
            }
        }

        /// <summary>Commits every seat, drives the turn to him and starts the printed action.</summary>
        internal static IEnumerator BeginHanu(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default,
            CardDefinitionId allyFace = default)
        {
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            yield return CommitEverySeatOfHis(root, face, enemyFace, allyFace);
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
        /// standing and about the round - the half of a cross-seat prompt no single-seat journey can
        /// see, and the half his set needs most: the co-moves put the DISTANCE to a friendly hero's own
        /// seat and his Ultimate re-homes a whole action to his.
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

        /// <summary>
        /// Milestone 8.70's greyed set: every bullet a standing modal draws with a label, a place and
        /// no command behind it.
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

        /// <summary>One card of this hero's, by the face it prints - the handle an Active effect is read by.</summary>
        internal static CardInstanceId CardIdOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definition) =>
            runtime.Cards
                .Single(card => card.OwnerId.Equals(heroId) &&
                    card.DefinitionReference.DefinitionId.Equals(definition))
                .Id;

        /// <summary>Every card of this hero's currently in hand, as definition ids.</summary>
        internal static string[] HeldBy(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// The Initiative this hero's revealed card is drawn with on their own side rail - milestone
        /// 8.55's badge, which reads the CARD's number and the item bonus as two numbers. Hurry up!
        /// replaces the first of them, so the badge is where a player reads the rewrite.
        /// </summary>
        internal static string InitiativeBadgeOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            var hero = runtime.HeroOverviewsView.Model.Heroes
                .SingleOrDefault(value => value.HeroId.Equals(heroId));
            Assert.That(hero, Is.Not.Null, "No rail row is drawn for " + heroId.Value + ".");
            Assert.That(hero.CurrentCard, Is.Not.Null,
                "No revealed card is drawn on " + heroId.Value + "'s rail. " + Remaining(runtime));
            return hero.CurrentCard.CardInitiativeLabel;
        }

        /// <summary>
        /// Ends the NAMED hero's turn on the control the round strip draws, through milestone 8.35g's
        /// two presses - the one that finishes the resolution and the one that hands the turn on - and
        /// stops as soon as a pause opens, which is what an "End of turn:" body does.
        ///
        /// <para>The hero is named rather than taken from the projection because the second press
        /// hands the turn to SOMEBODY ELSE, and a loop that kept pressing would spend that player's
        /// turn as well - measured on the first Unity run of this milestone, where it ran a whole round
        /// past the assertion that was waiting for the next actor.</para>
        /// </summary>
        internal static IEnumerator EndHisTurn(
            MatchSceneCompositionRoot root, HeroId actor, int presses = 4)
        {
            var runtime = root.Runtime;
            for (var press = 0; press < presses; press++)
            {
                if (runtime.PendingEffectChoice != null)
                {
                    yield break;
                }

                // A turn whose resolution has finished holds the round with an EMPTY selection until
                // its own player releases it (milestone 8.35g), so "nobody is acting" is NOT the
                // exit - only somebody ELSE acting is. Stopping there is what keeps the loop from
                // spending the next player's turn as well, which is what the first Unity run of this
                // milestone measured against three journeys waiting for the next actor.
                if (runtime.SelectedActor.IsDefined &&
                    !runtime.SelectedActor.ActorId.Equals(actor))
                {
                    yield break;
                }

                if (runtime.RoundView.Model.EndTurnInvocation != null)
                {
                    UiPlayer.Click(
                        UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }
                else if (!UiPlayer.ReleaseFinishedTurn(runtime))
                {
                    break;
                }

                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.PendingEffectChoice != null ||
                    !runtime.SelectedActor.IsDefined ||
                    !runtime.SelectedActor.ActorId.Equals(actor),
                Is.True,
                "and the turn really ended, or stopped on a pause its own end-of-turn text " +
                "opened. " + Remaining(runtime));
        }

        /// <summary>
        /// Ends the WHOLE turn - every actor in rulebook p.7's Initiative order, each on the control
        /// the round strip draws them - and stops the moment a pause opens or the turn is over.
        ///
        /// <para><b>It is the whole turn and not his own, and that is the rulebook rather than a
        /// convenience.</b> <c>TimingCheckpoint.TurnEnded</c> is the close of the turn every hero
        /// resolves a card in, so an "End of turn:" body of his fires after the LAST actor has acted
        /// and not after his own release - measured on this milestone's second Unity run, where a
        /// loop that stopped at his own release left three journeys reading a board the swap back had
        /// not reached yet.</para>
        /// </summary>
        internal static IEnumerator EndTheWholeTurn(
            MatchSceneCompositionRoot root, int presses = 10)
        {
            var runtime = root.Runtime;
            for (var press = 0; press < presses; press++)
            {
                if (runtime.PendingEffectChoice != null ||
                    runtime.Phase != RoundResolutionPhase.ResolvingTurn)
                {
                    yield break;
                }

                if (runtime.RoundView.Model.EndTurnInvocation != null)
                {
                    UiPlayer.Click(
                        UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }
                else if (!UiPlayer.ReleaseFinishedTurn(runtime))
                {
                    yield break;
                }

                yield return null;
                yield return null;
            }
        }

        /// <summary>
        /// How many of rulebook p.19's six directions leave this space with THREE free playable spaces
        /// along them - the count Fight and Flight's "move 3 spaces in a straight line" reads, and the
        /// one number an "if able" staging of his has to state rather than assume.
        /// </summary>
        internal static int StraightFilesOfThree(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate from)
        {
            var occupied = runtime.Projection.Heroes.Select(hero => hero.Coordinate)
                .Concat(runtime.Projection.Minions.Select(minion => minion.Coordinate))
                .Concat(runtime.Projection.Obstacles.Select(obstacle => obstacle.Coordinate))
                .ToArray();

            bool IsFree(HexCoordinate at) =>
                runtime.Projection.SelectedMap.Spaces.Any(space =>
                    space.Coordinate.Equals(at) && space.Kind == BoardMapSpaceKind.Playable) &&
                !occupied.Any(value => value.Equals(at));

            return from.GetNeighbors().Count(neighbour =>
                Enumerable.Range(1, 3).All(step => IsFree(new HexCoordinate(
                    from.Q + ((neighbour.Q - from.Q) * step),
                    from.R + ((neighbour.R - from.R) * step)))));
        }

        /// <summary>
        /// One filler decision, taken the way <c>NextRoundDecision</c> takes one - except that a
        /// COMMITMENT is submitted through the projected command rather than through the tray, because
        /// the shared filler commits by printed NAME and three faces of his share a first word.
        /// </summary>
        internal static IEnumerator NextDecision(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
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

        // -------------------------------------------------------------------------------------------
        // The component half his piece owns, on his own dealt board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// His own token stands at the configured Blue spawn as Hanu and only once, a camera ray on it
        /// points the tray at him, and his tray commits a face through rulebook p.7's own reveal.
        ///
        /// <para>The other half of his identity - that the 8.35z86 picker finds him by his three stars
        /// and each of his three printed roles, draws his own pick card on hover and hides him for a
        /// role he does not carry - is
        /// <c>PrivateLobbyPlayModeTests.HanuIsFoundByHisThreeStarsAndEachOfHisThreeRoles</c>, which his
        /// gate's own piece row names beside this one. It is the user's one named requirement for the
        /// twenty-first hero ("Make sure pick screen is correctly imeplented") and it is proved on the
        /// real selection screen rather than here, because that screen is a different scene.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisPieceSpawnsAsHanuAndHisTrayCommitsAndRevealsACard()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedEngagedStagedHanu,
                hand: new[]
                {
                    HanuCardContent.FightAndFlight, HanuCardContent.HurryUp,
                    HanuCardContent.HelpingHand, HanuCardContent.MonkeyTrick
                });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);

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
                root, NameOf(runtime, HanuCardContent.HelpingHand));
            Assert.That(runtime.GetCommitmentStatus(his),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown).Or
                    .EqualTo(TurnCardCommitmentStatus.Revealed),
                "CONFIRM FACEDOWN puts his card in p.7's turn area. " + Remaining(runtime));
            foreach (var seat in Enemies(runtime))
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
        /// <para>HA-06 splits his set exactly once: <b>Hurry up! prints a Defense 1 and NO Movement
        /// ribbon at all</b>, so the walk half skips it and the defend half does not. Every one of his
        /// other seventeen dealt faces prints both, and The Ultimate Trick prints no values and no
        /// action whatsoever - it cannot be committed, walked or discarded to defend.</para>
        ///
        /// <para>The loop runs on the Tier I scene for every face, including his Tier II and Tier III
        /// ones: a staged hand may take a card out of its owner's own DECK
        /// (<c>SandboxStagingAuthority.StageHands</c>), so the secondary of a face he has not levelled
        /// into is still his own printed secondary on his own card.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in HanuCardContent.DealtDefinitions
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
                Assert.That(
                    walks,
                    definition.Id.Equals(HanuCardContent.HurryUp) ? Is.False : Is.True,
                    "HA-06: Hurry up! is the one dealt face of his printing no Movement ribbon, and " +
                    "a missing icon is absent rather than zero.");

                if (walks)
                {
                    yield return StageHanu(
                        SceneCardContent.VerifiedEngagedStagedHanu,
                        hand: new[] { definition.Id, HanuCardContent.MonkeyTrick });
                    var root = Root();
                    var runtime = root.Runtime;
                    var his = Hanu(runtime);
                    yield return BeginHanu(root, definition.Id, ActionKind.Movement);
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
                // Shock (printed Initiative 8), so the attack he answers is the enemy's own turn. His
                // Greens print 1 and 2, which is the lowest Initiative in his whole set.
                var committed = definition.Id.Equals(HanuCardContent.MonkeyTrick)
                    ? HanuCardContent.MonkeyBusiness
                    : HanuCardContent.MonkeyTrick;
                yield return StageHanu(
                    SceneCardContent.VerifiedEngagedStagedHanu,
                    hand: new[] { definition.Id, committed },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: AdjacentEnemy);
                var defenseRoot = Root();
                var defenseRuntime = defenseRoot.Runtime;
                var defender = Hanu(defenseRuntime);
                var enemy = Enemies(defenseRuntime)[0];
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
        // His Silver: the rewrite, and the wall it raises
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hurry up!: "Set the printed Initiative value of an unresolved card of a hero in range to 11,
        /// until it is resolved or otherwise changes state."
        ///
        /// <para><b>Three things a player can see, on one board.</b> The candidate set is every hero in
        /// his printed Range 4 holding a faceup unresolved card and never himself (HA-Q1); the target's
        /// own rail badge reads <b>11</b> afterwards, where it read the number their card prints; and
        /// the ROUND REORDERS - the ally's committed Initiative 10 was next to act and the rewritten
        /// enemy's 8 was last, and after the rewrite the enemy acts first.</para>
        ///
        /// <para><b>And the rewind is refused.</b> Milestone 8.42e's interaction wall compares
        /// everything the acting seat's opponents own, and 8.71a appended a card's rewritten Initiative
        /// to that fingerprint - so a Hurry up! on an ENEMY puts the seat's undo floor above itself.
        /// Milestone 8.71e's own friendly case is the other half and lives in the engaged fixture.
        /// The seat-scoped square is a SEAT projection's fact and a Director sandbox publishes no seat
        /// of its own (<c>MatchSessionProjection.IsSeatUndoAvailable</c> is always false there), so the
        /// wall is read on the host's own seat projection - which is the value the square is drawn
        /// from - and the refusal detail is read beside it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HurryUpRewritesAnEnemysCardToElevenAndWallsHisOwnRewind()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAlly,
                hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.ExpertDuelist, ArienCardContent.NobleBlade });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var enemy = Enemies(runtime)[0];
            Assert.That(ally.IsDefined, Is.True, "Arrange: the ally board seats three heroes.");
            Assert.That(
                Distance(Position(runtime, his), Position(runtime, enemy)),
                Is.LessThanOrEqualTo(4),
                "Arrange: the enemy hero is inside his printed Range 4. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.HurryUp, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.ExpertDuelist);
            Assert.That(InitiativeBadgeOf(runtime, enemy), Is.EqualTo("8"),
                "Arrange: Shock prints Initiative 8, which is the number the rewrite replaces. " +
                Remaining(runtime));
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("10"),
                "Arrange: Expert Duelist prints 10, so before the rewrite the ALLY is next to act " +
                "and the enemy is last. " + Remaining(runtime));

            var target = runtime.PendingEffectChoice;
            Assert.That(target, Is.Not.Null,
                "The printed sentence's object is its own selection. " + Remaining(runtime));
            Assert.That(target.NodeId.Value, Is.EqualTo("hanu-hurry-up-target"));
            Assert.That(
                target.Candidates.Select(candidate => candidate.Value.HeroId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { ally.Value, enemy.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                "\"a hero in range\" names no side (HA-Q1), and Hanu himself is never one of them - " +
                "p.10 affects you only when the text says so. " + Remaining(runtime));

            yield return Pick(
                root,
                target.Candidates.Single(candidate => candidate.Value.HeroId.Equals(enemy)));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The rewrite asks nobody anything - it is the sentence's verb. " +
                Remaining(runtime));
            Assert.That(InitiativeBadgeOf(runtime, enemy), Is.EqualTo("11"),
                "and the target's own rail badge reads the printed eleven where it read 8. " +
                Remaining(runtime));
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("10"),
                "while nobody else's moved. " + Remaining(runtime));
            SeatsAgree(runtime, "the rewrite");

            var hisSeat = SeatOf(runtime, his);
            Assert.That(runtime.Host.ProjectionForSeat(hisSeat).IsSeatUndoAvailable, Is.False,
                "8.42e's interaction wall: the rewrite changed something an opponent owns, so this " +
                "seat's own rewind may not reach back through it. " + Remaining(runtime));

            yield return EndHisTurn(root, his);
            // The next actor is read WITHOUT ending anybody's turn, because who it is is the
            // assertion: a helper that pressed its way to a named seat would pass on a board the
            // rewrite had not reordered at all.
            for (var frame = 0; frame < 8 && !runtime.SelectedActor.IsDefined; frame++)
            {
                yield return null;
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemy),
                "p.7 checks the Initiative again after every resolution, so the hero whose 8 became " +
                "an 11 acts before the ally's 10 - the round reordered live, which is what the " +
                "printed parenthesis reminds a player of. " + Remaining(runtime));
            SeatsAgree(runtime, "the reordered round");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // His Gold: the straight line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Fight and Flight: "Target a unit adjacent to you. If the target is not defeated, After the
        /// attack: If able, move 3 spaces in a straight line."
        ///
        /// <para><b>Two boards, and the difference between them is the only thing the rider reads.</b>
        /// On the engagement he stands at (-3,1), where exactly two files of three free spaces leave his
        /// square, so the offer is the two ends of those files and nothing between them - "3 spaces" is
        /// a COUNT under p.11 and both of <c>choose-line-destination@1</c>'s bounds are three. On a
        /// board where no such file exists the printed "If able" makes an empty offer a step NOT TAKEN:
        /// no control is drawn, no pause stands, and the Attack completes anyway.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FightAndFlightMovesHimThreeSpacesInALineOrDoesNothingAtAll()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedEngagedStagedHanu,
                hand: new[] { HanuCardContent.FightAndFlight, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                enemySpace: AdjacentEnemy);
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];
            Assert.That(Distance(Position(runtime, his), Position(runtime, enemy)), Is.EqualTo(1),
                "Arrange: \"a unit adjacent to you\". " + Remaining(runtime));
            Assert.That(StraightFilesOfThree(runtime, Position(runtime, his)), Is.EqualTo(1),
                "Arrange: ONE of p.19's six directions leaves his square with three free playable " +
                "spaces. Milestone 8.71h measured two on the untouched engagement - through (-2,1) " +
                "and through (-4,1) - and the enemy hero this journey stands beside him occupies " +
                "the first of them, which is what makes the offer below an exact set. " +
                Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.FightAndFlight, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "His Gold prints no Range icon at all, so p.13 reads it as adjacency by rule and " +
                "the target question opens straight away. " + Remaining(runtime));
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection, Remaining(runtime));
            var stood = Position(runtime, his);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                "Arrange: \"If the target is not defeated\" - his printed Attack 2 is answered by a " +
                "Defense that survives it. " + Remaining(runtime));
            var line = runtime.PendingEffectChoice;
            Assert.That(line, Is.Not.Null,
                "p.14's step 7 reaches the rider, which is what CardTextTiming.AfterTheAttack says. " +
                Remaining(runtime));
            Assert.That(line.NodeId.Value, Is.EqualTo("hanu-flight-line"));
            Assert.That(line.DeclineInvocation, Is.Null,
                "\"If able\" is a step not taken rather than a decline a player presses - the " +
                "optionality lives in the offer. " + Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Is.Not.Empty, Remaining(runtime));
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, stood) == 3),
                Is.True,
                "\"move 3 spaces in a straight line\" is a count, not a ceiling: every candidate is " +
                "exactly three away. Offered: " +
                string.Join(", ", OfferedSpaces(runtime).Select(space => space.ToString())) + ". " +
                Remaining(runtime));

            yield return Space(root);
            Assert.That(Distance(Position(runtime, his), stood), Is.EqualTo(3),
                "and the file is walked whole. " + Remaining(runtime));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(Position(runtime, his)));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            SeatsAgree(runtime, "the straight line");
            Healthy(runtime);

            // The "if able" nothing, on a MEASURED board: three of this space's six neighbours are
            // the map's own water ring, rulebook p.18's terrain closes a fourth, the enemy he is
            // attacking stands in a fifth, and the friendly ranged minion is staged into the sixth -
            // so not one of p.19's six directions leaves a file of three free spaces, and the rider
            // is a step not taken rather than an impossible one.
            yield return StageHanu(
                SceneCardContent.VerifiedEngagedStagedHanu,
                hand: new[] { HanuCardContent.FightAndFlight, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                hisSpace: new HexCoordinate(1, -3),
                enemySpace: new HexCoordinate(0, -3),
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(
                        new MinionId("blue-ranged-01"), new HexCoordinate(2, -3))
                });
            var edgeRoot = Root();
            var edgeRuntime = edgeRoot.Runtime;
            var edgeHanu = Hanu(edgeRuntime);
            var edgeEnemy = Enemies(edgeRuntime)[0];
            var edgeStood = Position(edgeRuntime, edgeHanu);
            Assert.That(StraightFilesOfThree(edgeRuntime, edgeStood), Is.Zero,
                "Arrange: no direction out of his square holds three free playable spaces. " +
                Remaining(edgeRuntime));

            yield return BeginHanu(
                edgeRoot, HanuCardContent.FightAndFlight, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    edgeRoot.Hud,
                    HeroView(edgeEnemy).transform.position),
                Is.True,
                Remaining(edgeRuntime));
            yield return null;
            yield return ClickDefenseFace(edgeRoot, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            Assert.That(edgeRuntime.PendingEffectChoice, Is.Null,
                "HA-Q7: \"If able\" makes an empty offer nothing rather than an impossible step, so " +
                "no control is drawn at all. " + Remaining(edgeRuntime));
            Assert.That(Position(edgeRuntime, edgeHanu), Is.EqualTo(edgeStood),
                "and he stands exactly where he stood. " + Remaining(edgeRuntime));
            SeatsAgree(edgeRuntime, "the refused line");
            Healthy(edgeRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // His Red Tier I: the modal at one bullet
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Helping Hand: "Choose one - • Target a unit adjacent to you. • Target a hero in range,
        /// adjacent to your friendly hero."
        ///
        /// <para><b>The ally board, because the second bullet's predicate reads a HERO.</b>
        /// <c>AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor</c> asks
        /// <c>RosterPresenceQuery</c> for a <c>RosterUnitKind.Hero</c> on the actor's own side beside
        /// the candidate, with Hanu himself excluded - so the friendly melee minion standing in his own
        /// ring does not satisfy it, and a one-versus-one board would grey this bullet for a reason
        /// belonging to the staging. Arien stands at (-1,1) and the enemy hero beside him at (-1,2),
        /// three from Hanu and inside the printed Range 3.</para>
        ///
        /// <para><b>Both bullets are taken, each in its own run</b>, and the second board is the one
        /// that GREYS the narrowed bullet: the same face with the enemy hero standing beside nobody at
        /// all, where milestone 8.70's channel draws the bullet with its label and no command behind
        /// it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HelpingHandTakesEitherPrintedBulletAndGreysTheOneNamingNobody()
        {
            foreach (var friendlyBullet in new[] { false, true })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAlly,
                    hand: new[] { HanuCardContent.HelpingHand, HanuCardContent.MonkeyTrick },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                    enemySpace: EnemyBesideTheAlly);
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var ally = Ally(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(
                    Distance(Position(runtime, ally), Position(runtime, enemy)), Is.EqualTo(1),
                    "Arrange: the enemy hero stands beside his friendly HERO, which is the only " +
                    "thing the second bullet's predicate reads. " + Remaining(runtime));
                Assert.That(
                    Distance(Position(runtime, his), Position(runtime, enemy)), Is.EqualTo(3),
                    "Arrange: and inside the printed Range 3 rather than in his own ring. " +
                    Remaining(runtime));

                yield return BeginHanu(
                    root, HanuCardContent.HelpingHand, ActionKind.Attack,
                    WaspCardContent.Shock, ArienCardContent.NobleBlade);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(new[] { "hanu-red-adjacent-bullet", "hanu-red-friendly-bullet" })
                        .AsCollection,
                    "p.10's \"Choose one -\" is drawn as its two printed bullets, and both name " +
                    "somebody on this board. " + Remaining(runtime));
                yield return Bullet(
                    root,
                    friendlyBullet ? "hanu-red-friendly-bullet" : "hanu-red-adjacent-bullet");

                var target = runtime.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null,
                    "Taking a bullet reaches p.14's own target question. " + Remaining(runtime));
                if (friendlyBullet)
                {
                    Assert.That(target.LegalTargetHeroIds.ToArray(),
                        Is.EqualTo(new[] { enemy }).AsCollection,
                        "\"Target a HERO in range, adjacent to your friendly hero\". " +
                        Remaining(runtime));
                    Assert.That(target.LegalTargetMinionIds, Is.Empty,
                        "and the noun is a hero, so no minion is offered. " + Remaining(runtime));
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            HeroView(enemy).transform.position),
                        Is.True,
                        Remaining(runtime));
                    yield return null;
                    yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                }
                else
                {
                    Assert.That(target.LegalTargetHeroIds, Is.Empty,
                        "\"a unit adjacent to you\" - nothing but minions stands in his own ring on " +
                        "this staging. " + Remaining(runtime));
                    Assert.That(
                        target.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                        Contains.Item(RedRanged.Value),
                        "and the enemy ranged minion the engagement leaves beside him is one. " +
                        Remaining(runtime));
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            MinionView(RedRanged).transform.position),
                        Is.True,
                        Remaining(runtime));
                }

                yield return null;
                yield return null;
                SeatsAgree(runtime, friendlyBullet ? "the friendly bullet" : "the adjacent bullet");
                Healthy(runtime);
            }

            // The narrowed bullet on a board where it names nobody: the enemy hero stands three from
            // Hanu and beside no hero of his at all, so milestone 8.70 greys it at the choice.
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAlly,
                hand: new[] { HanuCardContent.HelpingHand, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                enemySpace: EnemyBesideTheAlly,
                allySpace: new HexCoordinate(-4, 1));
            var greyRoot = Root();
            var greyRuntime = greyRoot.Runtime;
            var greyAlly = Ally(greyRuntime);
            var greyEnemy = Enemies(greyRuntime)[0];
            Assert.That(
                Distance(Position(greyRuntime, greyAlly), Position(greyRuntime, greyEnemy)),
                Is.GreaterThan(1),
                "Arrange: his friendly hero stands nowhere near the enemy. " +
                Remaining(greyRuntime));

            yield return BeginHanu(
                greyRoot, HanuCardContent.HelpingHand, ActionKind.Attack,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            Assert.That(GreyedBullets(greyRuntime),
                Is.EqualTo(new[] { "hanu-red-friendly-bullet" }).AsCollection,
                "The narrowed bullet names nobody on this board, so it is greyed rather than " +
                "offered, taken and spent on nothing. " + Remaining(greyRuntime));
            Assert.That(OfferedBullets(greyRuntime),
                Is.EqualTo(new[] { "hanu-red-adjacent-bullet" }).AsCollection,
                "and the bullet whose sentence does name a unit is the answer left to give. " +
                Remaining(greyRuntime));
            Assert.That(
                greyRuntime.RoundView.Model.UnavailableEffectChoices, Has.Count.EqualTo(1),
                "THE RENDERED TIER AGREES WITH THE AUTHORITY, which is the half a headless case " +
                "cannot see: the window draws the greyed bullet with its label and its place and no " +
                "command behind it. " + Remaining(greyRuntime));
            yield return Bullet(greyRoot, "hanu-red-adjacent-bullet");
            Assert.That(greyRuntime.PendingAttackTargetChoice, Is.Not.Null, Remaining(greyRuntime));
            SeatsAgree(greyRuntime, "the greyed bullet");
            Healthy(greyRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // His Blue Tier I: the journey there and back
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Unexpected Journey: "Swap with an enemy hero in radius. This turn: That hero is immune. End
        /// of turn: Swap with that hero regardless of radius and immunity."
        ///
        /// <para><b>Three moments, and a player meets all three in one turn.</b> The Skill exchanges the
        /// two pieces; the Active effect it raises is readable on the card that raised it and makes the
        /// swapped hero p.18-immune for the rest of the turn - measured by a SECOND action of Hanu's own
        /// in the same turn finding that hero missing from its candidates; and the turn's own end runs
        /// the swap back "regardless of radius and immunity", which is the one exchange in his set that
        /// asks neither question.</para>
        ///
        /// <para>The mid-turn immunity is read on Monkey Trick's own pair rather than on an Attack,
        /// because his Gold has already been spent and a second primary is not available: what is
        /// offered is a SECONDARY, and the honest reading of "that hero is immune" at this tier is the
        /// candidate set of the next thing that would target him.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator UnexpectedJourneySwapsWithTheEnemyWhoIsImmuneUntilTheSwapBack()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedEngagedStagedHanu,
                hand: new[] { HanuCardContent.UnexpectedJourney, HanuCardContent.HearNothing },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];
            var hisStood = Position(runtime, his);
            var theirStood = Position(runtime, enemy);
            Assert.That(Distance(hisStood, theirStood), Is.EqualTo(2),
                "Arrange: inside the printed Radius 2, which is the smallest any swap of his " +
                "prints. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.UnexpectedJourney, ActionKind.Skill, WaspCardContent.Shock);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("journey-subject"));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "\"an ENEMY hero in radius\" - his own side is not a candidate and neither is he. " +
                Remaining(runtime));
            yield return Pick(root, subject.Candidates.Single());

            Assert.That(Position(runtime, his), Is.EqualTo(theirStood),
                "p.19's Swap exchanges the two positions. " + Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(hisStood));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(theirStood),
                "and both pieces follow the authority.");
            Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(hisStood));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, his, HanuCardContent.UnexpectedJourney)),
                Is.True,
                "and the card that printed the two keywords carries the Active effect that holds " +
                "them, where a player reads it. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Nothing else is asked at resolution: the second and third sentences are the " +
                "effect's, not the action's. " + Remaining(runtime));
            SeatsAgree(runtime, "the journey out");

            // "This turn: That hero is immune." - the printed primary is spent, and what a player
            // reads instead is the offer of the NEXT thing that would target that hero: p.18's
            // immunity keeps him out of every candidate set while the effect stands, which every
            // consumer enforces by leaving the piece out rather than by drawing a greyed control.
            Assert.That(
                PrimaryIsOffered(runtime, HanuCardContent.UnexpectedJourney), Is.False,
                "The primary it was spent on is spent. " + Remaining(runtime));

            yield return EndTheWholeTurn(root);
            yield return null;
            yield return null;
            Assert.That(Position(runtime, his), Is.EqualTo(hisStood),
                "\"End of turn: Swap with that hero regardless of radius and immunity\" - the " +
                "mandatory swap back, which asks neither question. " + Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(theirStood));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(hisStood));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, his, HanuCardContent.UnexpectedJourney)),
                Is.False,
                "and the effect expires at the same checkpoint it fires on, so it stands for " +
                "exactly one turn. " + Remaining(runtime));
            SeatsAgree(runtime, "the journey back");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // His Green Tier I: the friendly pair
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Monkey Trick: "Swap two friendly units in radius."
        ///
        /// <para><b>Two clicks on one control, and neither of them is Hanu.</b> p.17's Friendly is
        /// another unit on his own team, so his own piece is not a candidate for the pair he swaps; the
        /// printed Radius 1 is read off the card by the selection rather than written into the
        /// definition; and the pair is unordered, so the two clicks are the same question asked twice
        /// rather than a first and a second.</para>
        ///
        /// <para>The engagement leaves one friendly minion in his ring, so the second member is staged
        /// into it: <c>blue-ranged-01</c> is moved from (-3,4) to a space beside him. It is the RANGED
        /// minion rather than the heavy one, and that is measured rather than arbitrary - p.18 makes a
        /// heavy minion immune to being swapped or placed, so staging the heavy beside him leaves the
        /// pair one candidate short and greys the whole printed primary, which is what the first Unity
        /// run of this journey found.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MonkeyTrickSwapsTwoFriendlyUnitsInsideItsPrintedRadius()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedEngagedStagedHanu,
                hand: new[] { HanuCardContent.MonkeyTrick, HanuCardContent.HurryUp },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(BlueRanged, new HexCoordinate(-2, 1))
                });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            Assert.That(
                Distance(MinionView(BlueRanged).Occupancy.Coordinate, Position(runtime, his)),
                Is.EqualTo(1),
                "Arrange: two friendly units stand inside the printed Radius 1. " +
                Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.MonkeyTrick, ActionKind.Skill, WaspCardContent.Shock);
            var pair = runtime.PendingEffectChoice;
            Assert.That(pair, Is.Not.Null, Remaining(runtime));
            Assert.That(pair.NodeId.Value, Is.EqualTo("green-friendly-swap-pair"));
            Assert.That(
                pair.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(his)),
                Is.False,
                "p.17: \"A friendly unit means another unit on your team. It never includes you.\" " +
                Remaining(runtime));
            Assert.That(
                pair.Candidates.Select(candidate => candidate.Value.MinionId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { BlueRanged.Value, BlueMelee.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                "and the two friendly minions inside the printed Radius 1 are the whole offer. " +
                Remaining(runtime));

            var rangedWas = MinionView(BlueRanged).Occupancy.Coordinate;
            var meleeWas = MinionView(BlueMelee).Occupancy.Coordinate;
            yield return Pick(
                root,
                pair.Candidates.Single(candidate =>
                    candidate.Value.MinionId.Equals(BlueRanged)));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"two friendly units\" - a pair takes a second click. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("green-friendly-swap-pair"),
                "on the same control, because the pair is unordered. " + Remaining(runtime));
            yield return Pick(
                root,
                runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.MinionId.Equals(BlueMelee)));

            Assert.That(MinionView(BlueRanged).Occupancy.Coordinate, Is.EqualTo(meleeWas),
                "p.19's Swap exchanges the two, and neither of them is him. " + Remaining(runtime));
            Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(rangedWas));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Monkey Trick prints no second sentence. " + Remaining(runtime));
            SeatsAgree(runtime, "the friendly pair");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The whole printed set, drawn
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.20c's standard: every verified face he prints, drawn at the validated player
        /// viewport, is exactly one <c>CardFootprint</c> and shows its title, values, action, rules text
        /// and history whole. Measured over all NINETEEN, the boxed Handicap included - a card nobody
        /// can play is still a card somebody can look at.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryHanuFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedHanu);
            var root = Root();
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Hanu", HanuCardContent.AllDefinitions, root.Hud);
        }
    }
}
